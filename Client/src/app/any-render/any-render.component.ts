import { Component, OnInit, Input } from '@angular/core';
import { BackendService } from './../backend.service';
import { RenderContext } from './../data.model';

@Component({
  selector: 'app-any-render',
  templateUrl: './any-render.component.html',
  styleUrls: ['./any-render.component.css'],
})
export class AnyRenderComponent implements OnInit {
  @Input() editable: boolean;
  @Input() target: any;
  @Input() context: RenderContext;

  constructor(private backend: BackendService) {}

  ngOnInit() {}

  linkClick(id: string) {
    this.backend.sendMessage(
      {
        type: 'link',
        id,
      }
    );
  }
}
