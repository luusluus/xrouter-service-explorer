import { Component, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { Location } from '@angular/common';
import { XrouterApiService } from '../shared/services/xrouter.service';
import { Router, ActivatedRoute, NavigationEnd } from '@angular/router';
import { isNullOrUndefined } from 'util';
import { NgForm } from '@angular/forms';
import { finalize } from 'rxjs/operators';

@Component({
  selector: 'app-view-xr-service',
  templateUrl: './view-xr-service.component.html',
  styleUrls: ['./view-xr-service.component.css']
})
export class ViewXrServiceComponent implements OnInit, OnDestroy {
  navigationSubscription;
  loading:boolean = true;
  serviceName:string;
  result:any;
  parametervalues:string[];

  @ViewChild('serviceForm') serviceForm: NgForm;
  serviceResult:any;
  resultLoading:boolean;

  constructor(
    private xrouterApiService:XrouterApiService,
    private router:Router,
    private route:ActivatedRoute, 
    private location:Location
    ) 
    { 
      route.params.subscribe(p => {
        this.serviceName = p['name'];
        if (isNullOrUndefined(this.serviceName)) {
          router.navigate(['']);
          return; 
        }
      });

      this.navigationSubscription = this.router.events.subscribe((e:any) => {
        if(e instanceof NavigationEnd){
          this.initializeData();
        }
      });
    }

  private initializeData(){
    this.xrouterApiService.GetServiceInfo(this.serviceName)
      .subscribe(result => {
        this.result = result;
        this.location.replaceState("/xcloud-services/" + this.serviceName + "/" + this.result.node.nodePubKey);
        this.serviceName = this.serviceName.replace("xrs::","");
        if(this.result.service.parametersList)
          this.parametervalues = new Array<string>(this.result.service.parametersList.length);

        this.loading = false;
        this.resultLoading = false;
      },
      error => {
        this.router.navigate(['/error'], {queryParams: error});
      });
  }
  ngOnInit() {}

  onSubmit() {  
    this.resultLoading = true; 
    this.xrouterApiService.Service(new ServiceRequest('xrs::' + this.serviceName, this.parametervalues, 1))
    .pipe(
      finalize(() => {
        this.resultLoading = false;
    }))  
    .subscribe(result => {
        this.serviceResult = result;
      },
      error => {
        this.router.navigate(['/error'], {queryParams: error})
      });    
  }

  ngOnDestroy(){
    if (this.navigationSubscription) {
      this.navigationSubscription.unsubscribe();
    }
  }

}

class ServiceRequest{
  constructor(
    service:string,
    parameters:string[],
    nodecount:number
  ) { 
    this.service = service;
    this.parameters = parameters;
    this.nodecount = nodecount;
   }
  
   service:string;
   parameters:string[];
   nodecount:number;
}


