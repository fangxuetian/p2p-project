import { UPDATE_WALLET_INFO, UPDATE_USER_INFO, UPDATE_USER_INFO_BY_NAME, UPDATE_BANNER_INFO } from "../actions/usercenter.js"
import { UPDATE_BANK_CARDS } from "../actions/bankcard.js"
import {UPDATE_ADDRESS} from "../actions/order_address.js"
import Immutable from "immutable"

const initialState = {
	walletInfo: {
		idleMoney: 0,
		lockedMoney: 0,
		investingMoney: 0,
		profitingMoney: 0,
		lotteriesValue: 0,
		totalCharge: 0,
		totalInvestment: 0,
		totalProfit: 0,
		totalWithdraw: 0
	},
	userInfo: {
		userName: "",
		prevLoginTime: "",
		realName: "",
		idCardNumber: "",
		nickName: "",
		mobile: "",
		email: "",
		qq: "",
		sex: "",
		birthday: "",
		area: "",
		address: "",
		invitationCode: "",
		hasTransactPassword: false,
		groupName: "",
		isLoaner: false,
		identityId: "",
        point:""
	},
	bankCards: [],
	bannerInfo: [],
    orderAddress: []
}

export default function userCenter(state = initialState, action) {
	switch (action.type) {
	case UPDATE_WALLET_INFO:
		return Immutable.fromJS(state).mergeIn([ "walletInfo" ], action.walletInfo).toJS();
	case UPDATE_USER_INFO:
		return Immutable.fromJS(state).mergeIn([ "userInfo" ], action.userInfo).toJS();
	case UPDATE_USER_INFO_BY_NAME:
		return Immutable.fromJS(state).setIn([ "userInfo", action.name ], action.value).toJS();
	case UPDATE_BANK_CARDS:
	    return Immutable.fromJS(state).setIn([ "bankCards"], action.bankCards).toJS();
	case UPDATE_BANNER_INFO:
	    return Immutable.fromJS(state).setIn([ "bannerInfo"],action.bannerInfo).toJS();
	case UPDATE_ADDRESS:
	    return Immutable.fromJS(state).setIn([ "orderAddress" ], action.orderAddress).toJS();
	default:
		return state;
	}
}