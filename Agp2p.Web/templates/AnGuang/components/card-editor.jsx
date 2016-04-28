import React from "react";
import { bankList } from "../js/bank-list.jsx";
import { appendBankCard, modifyBankCard } from "../actions/bankcard.js";

import alert from "../components/tips_alert.js";
import mask from "../js/mask.js";

class CardEditor extends React.Component {
	constructor(props) {
		super(props);
		this.state = this.genStateByValue(props.defaultValue);
	}
    componentWillReceiveProps(nextProps) {
        this.setState(this.genStateByValue(nextProps.value));
    }
	genStateByValue(val) {
		var state = {
			bank: "",
			cardNumber: "",
			cardNumber2: ""
		};
		if (val) {
			state.bank = val.bankName;
			state.cardNumber = val.cardNumber;
		}
		return state;
	}
	doSaveCard() {
		if (!this.state.bank) {
			alert("请先选择银行");
			return;
		}
		if (!this.state.cardNumber) {
			alert("请先输入卡号");
			return;
		}
		if (!this.props.value) {
			if (this.state.cardNumber2 != this.state.cardNumber) {
				alert("两次输入的卡号不一致");
				return;
			}
			var promise = this.props.dispatch(appendBankCard(this.state.cardNumber, this.state.bank));
			promise.done(this.props.onOperationSuccess);
		} else {
			var promise = this.props.dispatch(modifyBankCard(this.props.value.cardId, this.state.bank));
			promise.done(this.props.onOperationSuccess);
		}
	}
	render() {
		let creatingCard = !this.props.value, editingCard = !creatingCard;
		return (
			<div className={this.props.rootClass} style={this.props.style}>
				<ul className="list-unstyled">
					<li><span>开户名：</span><span style={this.props.realName ? null : {color: "red"}} >
						{this.props.realName || "（请先到 “个人中心 -> 安全中心” 进行实名认证）"}</span></li>
					<li><span>选择银行：</span><select className="bankSelect" value={this.state.bank}
						onChange={ev => this.setState({bank: ev.target.value})} disabled={!this.props.realName}>
						<option value="">请选择银行</option>
						{bankList.map(b => <option value={b} key={b}>{b}</option>)}
						</select></li>
					{editingCard
						? <li><span>银行卡号：</span>{mask(this.state.cardNumber, 2, 4)}</li>
						: <li><span>银行卡号：</span><input type="text" value={this.state.cardNumber}
						onChange={ev => this.setState({cardNumber: ev.target.value})} disabled={!this.props.realName} /><span style={{color: 'red', marginLeft: '10px'}}>*</span></li>}
					{editingCard ? null :
					<li><span>确认卡号：</span><input type="text" value={this.state.cardNumber2}
						onChange={ev => this.setState({cardNumber2: ev.target.value})} disabled={!this.props.realName} /><span style={{color: 'red', marginLeft: '10px'}}>*</span></li>}
				</ul>
				{creatingCard ? null :
					<button type="button" className="cancel-btn" onClick={ev => this.props.onOperationSuccess()}>取 消</button>}
				<button type="button" onClick={ev => this.doSaveCard()}
					disabled={!this.props.realName}>{creatingCard ? "提 交" : "保 存"}</button>
			</div>
		);
	}
}

function mapStateToProps(state) {
	return {
		realName: state.userInfo.realName,
	};
}

import { connect } from 'react-redux';
export default connect(mapStateToProps)(CardEditor);