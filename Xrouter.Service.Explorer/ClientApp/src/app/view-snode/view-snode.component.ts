import { Component, OnInit, OnDestroy } from '@angular/core';
import { Location } from '@angular/common';
import { XrouterService } from '../shared/services/xrouter.service';
import { Router, ActivatedRoute } from '@angular/router';
import { isNullOrUndefined } from 'util';
import { takeUntil } from 'rxjs/operators';
import { Subject, forkJoin, Observable } from 'rxjs';
import { MyServiceNodesService } from '../shared/services/myservicenodes.service';
import { ServiceNodeService } from '../shared/services/snode.service';

@Component({
  selector: 'app-view-snode',
  templateUrl: './view-snode.component.html',
  styleUrls: ['./view-snode.component.css']
})
export class ViewSnodeComponent implements OnInit, OnDestroy {
  private readonly PAGE_SIZE = 6; 

  protected ngUnsubscribe: Subject<void> = new Subject<void>();
  loading:boolean;

  nodePubKey:string;
  service:string;
  xCloudServices:any;
  serviceNodeInfo:any;
  snodeVerified:boolean;

  query:any = {
    page: 1,
    pageSize: this.PAGE_SIZE,
  };

  constructor(
    private router:Router,
    private route:ActivatedRoute, 
    private xrouterApiService:XrouterService,
    private myServiceNodesService: MyServiceNodesService,
    private serviceNodeService: ServiceNodeService
  ) 
  { 
    route.params.subscribe(p => {
      this.nodePubKey = p['nodePubKey'];
      this.service = p['service'];
      if (isNullOrUndefined(this.nodePubKey)) {
        router.navigate(['']);
        return; 
      }
      this.loading = true;
    });
  }

  ngOnInit() {
    var observableIsServiceNodeVerified: Observable<boolean> = this.myServiceNodesService.isServiceNodeVerified(this.nodePubKey);
    var observableServiceNodeInfo: Observable<any> = this.serviceNodeService.GetNodeInfo(this.nodePubKey, this.service);

    forkJoin([observableIsServiceNodeVerified, observableServiceNodeInfo]).pipe(takeUntil(this.ngUnsubscribe)).subscribe(([verified, nodeInfo]) =>{
      this.loading = false;
      this.snodeVerified = verified;
      this.serviceNodeInfo = nodeInfo;
    }, err => {
      if(err.status == 404)
      this.router.navigate(['/error'], {queryParams: err});
    });
  }

  ngOnDestroy() {
    // This aborts all HTTP requests.
    this.ngUnsubscribe.next();
    // This completes the subject properlly.
    this.ngUnsubscribe.complete();
  }

  private populateXCloudServices(){
    this.serviceNodeService.FilterXCloudServiceServiceNode(this.query)
      .subscribe(result => {        
        this.xCloudServices = result;
      });
  }

  ngOnChanges(){
    this.initializeQuery();
	}

  onFilterChange() {
    this.query.page = 1; 
    this.populateXCloudServices();
  }

  private initializeQuery(){}

  resetFilter() {
    this.query = {
      page: 1,
      pageSize: this.PAGE_SIZE,
    };
    this.populateXCloudServices();
  }

  onPageChange(page) {
    this.query.page = page;
    this.populateXCloudServices();
  }

}