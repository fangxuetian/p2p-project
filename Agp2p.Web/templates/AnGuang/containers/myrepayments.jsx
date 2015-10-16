﻿import React from "react";

import HorizontalPicker from "../components/horizontal-picker.jsx"
import DatePicker from "../components/date-picker.jsx"
import Table from "../components/repayments-table.jsx"
import Pagination from "../components/pagination.jsx"

export default class MyTransaction extends React.Component {
    constructor(props) {
        super(props);
        this.state = {type: 1, startTime: "", endTime: "", pageIndex: 0, onPageLoaded: pageCount => this.setState({pageCount: pageCount})};
    }
    render() {
        return (
            <div>
                <div className="controls">
                    <HorizontalPicker onTypeChange={newType => this.setState({type: newType}) } enumFullName="Agp2p.Common.Agp2pEnums+MyRepaymentQueryTypeEnum" />
                    <DatePicker onStartTimeChange={newStartTime => this.setState({startTime: newStartTime})} onEndTimeChange={newEndTime => this.setState({endTime: newEndTime})}/>
                    <div style={{clear: "both"}}></div>
                </div>
                <Table
                    type={this.state.type}
                    pageIndex={this.state.pageIndex}
                    startTime={this.state.startTime}
                    endTime={this.state.endTime}
                    onPageLoaded={this.state.onPageLoaded} />
                <Pagination pageIndex={this.state.pageIndex} pageCount={this.state.pageCount}
                    onPageSelected={pageIndex => this.setState({pageIndex: pageIndex})}/>
            </div>);
    }
}