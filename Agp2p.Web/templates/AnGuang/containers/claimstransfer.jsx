import React from "react";
import { ajax } from "jquery";
import HorizontalPicker from "../components/horizontal-picker.jsx";
import Pagination from "../components/pagination.jsx";
import "../less/claimstransfer.less";
import confirm from "../components/tips_confirm.js";
import alert from "../components/tips_alert.js";

const ClaimStatusEnum = {
    Nontransferable : 1,
    Transferable : 2,
    NeedTransfer : 3,
    Completed : 10,
    CompletedUnpaid : 11,
    Transferred : 20,
    TransferredUnpaid : 21,
    Invalid : 30,
};

export default class ClaimsTransfer extends React.Component {
    constructor(props) {
        super(props);
        this.state = {claimQueryType:1, pageIndex:0, pageCount:0, claims: [],
            StaticClaimWithdrawAmount: '0.00', StaticClaimWithdrawCount: 0,
            BuyedStaticClaimAmount: '0.00', BuyedStaticClaimCount: 0};
    }
    componentDidMount() {
        this.fetchSummery();
        this.fetchClaims(1, 0);
    }
    fetchSummery() {
        let url = USER_CENTER_ASPX_PATH + "/AjaxQueryClaimTransferSummery";
        ajax({
            type: "GET",
            dataType: "json",
            contentType: "application/json",
            url: url,
            data: '',
            success: function(result) {
                let summery = JSON.parse(result.d);
                this.setState(summery);
            }.bind(this),
            error: function(xhr, status, err) {
                console.error(url, status, err.toString());
            }.bind(this)
        });
    }
    fetchClaims(type, pageIndex) {
        this.setState({claimQueryType: type, pageIndex: pageIndex});

        let url = USER_CENTER_ASPX_PATH + "/AjaxQueryStaticClaim", pageSize = 9;
        ajax({
            type: "POST",
            dataType: "json",
            contentType: "application/json",
            url: url,
            data: JSON.stringify({claimQueryType: type, pageIndex: pageIndex, pageSize: pageSize}),
            success: function(result) {
                let {totalCount, data} = JSON.parse(result.d);
                this.setState({claims: data, pageCount: Math.ceil(totalCount / pageSize)});
            }.bind(this),
            error: function(xhr, status, err) {
                console.error(url, status, err.toString());
            }.bind(this)
        });
    }
    applyForClaimTransfer(claimId) {
        confirm("转让成功后将会扣除一部分的手续费，是否继续？", () => {
            let url = USER_CENTER_ASPX_PATH + "/AjaxApplyForClaimTransfer";
            ajax({
                type: "POST",
                dataType: "json",
                contentType: "application/json",
                url: url,
                data: JSON.stringify({claimId}),
                success: function(result) {
                    if (result.d === "ok") {
                        alert("申请转让成功", () => {
                            this.fetchSummery();
                            this.fetchClaims(2, 0);
                        });
                    }
                }.bind(this),
                error: function(xhr, status, err) {
                    console.error(url, status, err.toString());
                    alert('申请债权转让失败：' + xhr.responseJSON.d);
                }.bind(this)
            });
        });
    }
    applyForCancelClaimTransfer(withdrawClaimId) {
        confirm("是否取消债权转让申请？", () => {
            let url = USER_CENTER_ASPX_PATH + "/AjaxApplyForCancelClaimTransfer";
            ajax({
                type: "POST",
                dataType: "json",
                contentType: "application/json",
                url: url,
                data: JSON.stringify({withdrawClaimId}),
                success: function(result) {
                    if (result.d === "ok") {
                        alert("撤回申请转让成功", () => {
                            this.fetchSummery();
                            this.fetchClaims(1, 0);
                        });
                    }
                }.bind(this),
                error: function(xhr, status, err) {
                    console.error(url, status, err.toString());
                    alert('取消债权转让申请失败：' + xhr.responseJSON.d);
                }.bind(this)
            });
        });
    }
    render() {
        return(
            <div className="claimsTransferPage">
                <div className="top-wrapper">
                    <div className="blue-container">
                        <p className="whatFor"><a href="javascript:">什么是债权转让？</a></p>
                        <ul className="claims-db list-unstyled list-inline ">
                            <li>
                                <p>成功转出金额</p>
                                <p>{this.state.StaticClaimWithdrawAmount + " 元"}</p>
                            </li>
                            <li>
                                <p>已转出债权笔数</p>
                                <p>{this.state.StaticClaimWithdrawCount}</p>
                            </li>
                            <li>
                                <p>成功转入金额</p>
                                <p>{this.state.BuyedStaticClaimAmount + " 元"}</p>
                            </li>
                            <li>
                                <p>已转入债权笔数</p>
                                <p>{this.state.BuyedStaticClaimCount}</p>
                            </li>
                        </ul>
                    </div>
                </div>
                <div className="bottom-wrapper">
                    <div className="warm-tips"><span>债权转让</span></div>
                    <HorizontalPicker onTypeChange={newType => this.fetchClaims(newType, 0) }
                        enumFullName="Agp2p.Common.Agp2pEnums+StaticClaimQueryEnum" value={this.state.claimQueryType} />
                    <div className="tb-container">
                        <table className="table claimsTransfer-tb">
                            <thead>
                                <tr>
                                    <th>债权编号</th>
                                    <th>收益项目</th>
                                    <th>年化利率</th>
                                    <th>本金</th>
                                    <th>状态</th>
                                    <th>创建时间</th>
                                    <th>下个收益日</th>
                                    <th>操作</th>
                                </tr>
                            </thead>
                            <tbody>
                            {this.state.claims.length == 0 ? <tr><td colSpan="8">暂无内容</td></tr> :
                                this.state.claims.map(c => 
                                    <tr key={c.id}>
                                        <td>{c.number}</td>
                                        <td>{c.profitingProject}</td>
                                        <td>{c.profitingYearly + "%"}</td>
                                        <td>{c.principal}</td>
                                        <td>{c.queryType}</td>
                                        <td>{c.createTime}</td>
                                        <td>{c.nextProfitDay}</td>
                                        <td>{c.status == ClaimStatusEnum.Nontransferable
                                            ? <a href="javascript:" onClick={ev => this.applyForClaimTransfer(c.id)}>申请转让</a>
                                            : (c.status == ClaimStatusEnum.NeedTransfer && c.buyerCount == 0
                                                && new Date(c.createTime).toJSON().slice(0,10) != new Date().toJSON().slice(0,10)
                                                ? <a href="javascript:" onClick={ev => this.applyForCancelClaimTransfer(c.id)}>撤回转让申请</a> : "")}</td>
                                    </tr>)
                            }
                            </tbody>
                        </table>
                    </div>
                    <Pagination pageIndex={this.state.pageIndex} pageCount={this.state.pageCount}
                                onPageSelected={pageIndex => this.fetchClaims(this.state.claimQueryType, pageIndex)}/>
                </div>
            </div>
        );
    }
}