﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MK94.SeeRaw
{
	public abstract class RendererBase
	{
		protected JsonSerializerOptions jsonOptions;
		protected Serializer serializer = new Serializer();

		private Context previousContext;

		protected RendererBase()
		{
			jsonOptions = new JsonSerializerOptions();
			jsonOptions.Converters.Add(new JsonStringEnumConverter());
		}

		public abstract object OnClientConnected(Server server, WebSocket websocket);
		public abstract void OnMessageReceived(object state, Server server, WebSocket websocket, string message);

		public virtual void DownloadFile(Stream stream, string fileName, string mimeType = "text/plain")
		{
			var path = SeeRawContext.localSeeRawContext.Value.Server.ServeFile(() => stream, fileName, mimeType, timeout: TimeSpan.FromSeconds(30));

			SeeRawContext.localSeeRawContext.Value.WebSocket.SendAsync(Encoding.ASCII.GetBytes(@$"{{ ""download"": ""{path}"" }}"), WebSocketMessageType.Text, true, default);
		}

		protected void ExecuteCallback(Server server, RenderRoot renderRoot, WebSocket webSocket, Dictionary<string, Delegate> callbacks, string message)
		{
			var deserialized = JsonSerializer.Deserialize<JsonElement>(message);

			var id = deserialized.GetProperty("id").GetString();
			var type = deserialized.GetProperty("type").GetString();

			if (callbacks.TryGetValue(id, out var @delegate))
			{
				if (type == "link" && @delegate is Action a)
					a();

				else if (type == "form")
				{
					var jsonArgs = deserialized.GetProperty("args");
					var parameters = @delegate.Method.GetParameters();
					var deserializedArgs = new List<object>();

					for (int i = 0; i < parameters.Length; i++)
					{
						// Hacky way to deserialize until https://github.com/dotnet/runtime/issues/31274 is implemented
						var jsonArg = JsonSerializer.Deserialize(jsonArgs[i].GetRawText(), parameters[i].ParameterType, jsonOptions);

						deserializedArgs.Add(jsonArg);
					}

					SetContext(server, renderRoot, webSocket);
					@delegate.DynamicInvoke(deserializedArgs.ToArray());
					ResetContext();
				}
				else UnknownMessage();
			}
			else UnknownMessage();

			void UnknownMessage()
			{
#if DEBUG
				Console.WriteLine("Unknown message " + message);
#endif
			}
		}

		protected void SetContext(Server server, RenderRoot renderRoot, WebSocket webSocket)
        {
			previousContext = SeeRawContext.localSeeRawContext.Value;

			SeeRawContext.localSeeRawContext.Value = new Context
			{
				Renderer = this,
				Server = server,
				RenderRoot = renderRoot,
				WebSocket = webSocket
			};
        }

		protected void ResetContext()
        {
			SeeRawContext.localSeeRawContext.Value = previousContext;
			previousContext = null;
		}

		public RendererBase WithSerializer<T>(ISerialize serializer) => WithSerializer(typeof(T), serializer);

		public RendererBase WithSerializer(Type type, ISerialize serializer)
		{
			this.serializer.serializers[type] = serializer;
			return this;
		}
	}

	public class SharedStateRenderer : RendererBase
	{
		private Server server;
		private RenderRoot state;
		private Dictionary<string, Delegate> callbacks = new Dictionary<string, Delegate>();

		public SharedStateRenderer(Server server, bool setGlobalContext, Action initialise)
		{
			this.server = server;
			state = new RenderRoot(r => Refresh());

			if (setGlobalContext)
				SetContext(server, state, null);

			if(initialise != null)
			{
				if (!setGlobalContext)
					SetContext(server, state, null);

				initialise.Invoke();

				if (!setGlobalContext)
					ResetContext();
            }
		}

		public override object OnClientConnected(Server server, WebSocket websocket) { Refresh(); return null; }
		public override void OnMessageReceived(object state, Server server, WebSocket websocket, string message) => ExecuteCallback(server, this.state, websocket, callbacks, message);

		public void Refresh()
		{
			callbacks.Clear();
			server.Broadcast(serializer.SerializeState(state, callbacks));
		}
    }

	public class PerClientRenderer : RendererBase
	{
		private class ClientState
		{
			public ClientState(RenderRoot state, Dictionary<string, Delegate> callbacks)
			{
				State = state;
				Callbacks = callbacks;
			}

			internal RenderRoot State { get; }

			internal Dictionary<string, Delegate> Callbacks { get; }


		}

		private readonly Action onClientConnected;

        public PerClientRenderer(Action onClientConnected)
		{
			this.onClientConnected = onClientConnected;
		}

        public override object OnClientConnected(Server server, WebSocket websocket)
        {
			var callbacks = new Dictionary<string, Delegate>();
			var renderRoot = new RenderRoot(r =>
			{
				var message = serializer.SerializeState(r, callbacks);
				Task.Run(() => websocket.SendAsync(message, WebSocketMessageType.Text, true, default));
			});

			var state = new ClientState(renderRoot, callbacks);

			SetContext(server, state.State, websocket);
			onClientConnected();
			ResetContext();

			return state;
        }

        public override void OnMessageReceived(object state, Server server, WebSocket websocket, string message)
        {
			var clientState = state as ClientState;

			ExecuteCallback(server, clientState.State, websocket, clientState.Callbacks, message);
        }
	}
}