import { Component, Input, OnInit } from '@angular/core';
import { Metadata, RenderContext, Option } from '@data/data.model';
import { RenderComponent } from './../../render.service';

@Component({
  selector: 'app-enum-render',
  templateUrl: './enum-render.component.html',
  styleUrls: ['./enum-render.component.css']
})
export class EnumRenderComponent implements RenderComponent {

  @Input() metadata: EnumMetadata;
  @Input() value: any;
  @Input() context: RenderContext;
  @Input() options: Option;

  expand(path) { return null; }
}

class EnumMetadata extends Metadata {
  values: any[]
}
