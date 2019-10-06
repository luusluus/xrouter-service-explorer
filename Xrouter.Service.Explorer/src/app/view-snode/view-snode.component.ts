import { Component, OnInit } from '@angular/core';
import { Location } from '@angular/common';
import { XrouterApiService } from '../shared/services/xrouter.service';
import { Router, ActivatedRoute } from '@angular/router';
import { isNullOrUndefined } from 'util';

@Component({
  selector: 'app-view-snode',
  templateUrl: './view-snode.component.html',
  styleUrls: ['./view-snode.component.css']
})
export class ViewSnodeComponent implements OnInit {
  loading:boolean;
  config:any;
  nodePubKey:string;
  service:string;
  selectedWalletName:string;
  selectedWallet:any;
  result:any;

  constructor(
    private xrouterApiService:XrouterApiService,
    private router:Router,
    private route:ActivatedRoute, 
    private location:Location
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
    this.xrouterApiService.GetNodeInfo(this.nodePubKey, this.service)
      .subscribe(result => {
        this.result = result;
        this.selectedWalletName = this.result.spvConfigs[0].spvWallet;
        this.onWalletChange();

        this.config = {
          itemsPerPage: 10,
          currentPage: 1,
          totalItems: this.result.services.length
        };
        this.loading = false;
      },
      error => {
        this.router.navigate(['/error'], {queryParams: error});
      });
  }

  onWalletChange(){
    this.selectedWallet = this.result.spvConfigs.find(c => c.spvWallet === this.selectedWalletName);
  }

  pageChanged(event){
    this.config.currentPage = event;
  }

}