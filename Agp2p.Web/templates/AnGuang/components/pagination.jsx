import React from "react";
import range from "lodash/utility/range";


class Pagination extends React.Component {
	constructor(props) {
        super(props);
        this.state = {};
    }
    componentWillReceiveProps (nextProps) {
    	if (nextProps.pageCount != this.props.pageCount &&
            nextProps.pageCount <= Math.max(nextProps.pageIndex, this.props.pageIndex) &&
            0 < nextProps.pageCount) {
    		this.props.onPageSelected(0);
    	}
    }
    genPaginationItem(index) {
    	if (index === "omit-left" || index === "omit-right") {
    		return <li key={index}><a>…</a></li>
    	} else if (this.props.pageIndex == index) {
    		return <li className="active" key={index}><a href="javascript:;">{index+1}<span className="sr-only">(current)</span></a></li>
    	} else {
    		return <li key={index}><a href="javascript:;" onClick={() => this.props.onPageSelected(index)}>{index+1}</a></li>
    	}
    }
    render() {
    	let {pageIndex, pageCount, keepShow} = this.props;
    	if (!pageCount) {
    		return null;
    	}

    	let init = range(pageCount).map(i => ({index: i, omit: true}));

    	// display page item by "keepShow"
    	let maxKeepShow = Math.min(keepShow, pageCount),
	    	oneSideKeepShow = Math.floor((maxKeepShow - 1) / 2),
	    	keepShowStartPoint = pageIndex - oneSideKeepShow;
	    
    	if (keepShowStartPoint < 0) {
    		keepShowStartPoint = 0;
    	} else if (pageCount < keepShowStartPoint + maxKeepShow) {
    		keepShowStartPoint = pageCount - maxKeepShow;
    	}
    	range(keepShowStartPoint, keepShowStartPoint + maxKeepShow).map(i => init[i].omit = false);

    	// display head and tail
    	init[0].omit = init[init.length - 1].omit = false;

    	let omitLeft = init.filter(item => item.omit && item.index < pageIndex);
    	let omitRight = init.filter(item => item.omit && pageIndex < item.index);

    	// display omited if only omit one item
    	if (omitLeft.length == 1) {
    		omitLeft[0].omit = false;
    	}
    	if (omitRight.length == 1) {
    		omitRight[0].omit = false;
    	}

    	// remove useless omit item
    	if (1 < omitLeft.length) {
    		omitLeft[0].index = "omit-left";
    		omitLeft[0].omit = false;
    	}
    	if (1 < omitRight.length) {
    		omitRight[0].index = "omit-right";
    		omitRight[0].omit = false;
    	}
    	let result = init.filter(item => !item.omit).map(item => item.index);

    	let isFirstPage = pageIndex == 0, isLastPage = pageCount == 0 || pageIndex == pageCount - 1;
    	return (
    		<div className="nav">
	    		<ul className="pagination">
		    		<li className={isFirstPage ? "disabled" : ""} key="prev">
		    			<a href="javascript:;" aria-label="Previous" onClick={isFirstPage ? null : () => this.props.onPageSelected(pageIndex-1)}>
			    			<span aria-hidden="true">&laquo;</span>
		    			</a>
	    			</li>
	    			{result.map(i => this.genPaginationItem(i))}
		    		<li className={isLastPage ? "disabled" : ""} key="next">
		    			<a href="javascript:;" aria-label="Next" onClick={isLastPage ? null : () => this.props.onPageSelected(pageIndex+1)}>
		    				<span aria-hidden="true">&raquo;</span>
	    				</a>
    				</li>
	    		</ul>
			</div>
		);
    }
}
Pagination.defaultProps = { keepShow: 5 } // should be odd number

export default Pagination;