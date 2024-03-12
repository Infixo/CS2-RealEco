import React from 'react'
import {useDataUpdate} from 'hookui-framework'
import $Panel from './panel'

const RowWithTwoColumns = ({left, right}) => {
	return (
	<div class="labels_L7Q row_S2v">
		<div class="row_S2v" style={{width: '70%'}}>{left}</div>
		<div class="row_S2v" style={{width: '30%', justifyContent: 'center'}}>{right}</div>
	</div>
	);
};

const RowWithThreeColumns = ({left, leftSmall, right1, flag1, right2, flag2}) => {
	const centerStyle = {
		width: right2 === undefined ? '30%' : '15%',
		justifyContent: 'center',
	};
	const right1text = `${right1}`;
	const right2text = `${right2}`;
	return (
	<div class="labels_L7Q row_S2v">
		<div class="row_S2v" style={{width: '70%', flexDirection: 'column'}}>
			<p>{left}</p>
			<p style={{fontSize: '80%'}}>{leftSmall}</p>
		</div>
		{flag1 ?
			<div class="row_S2v negative_YWY" style={centerStyle}>{right1text}</div> :
			<div class="row_S2v positive_zrK" style={centerStyle}>{right1text}</div>}
		{right2 && (
		flag2 ?
			<div class="row_S2v negative_YWY" style={centerStyle}>{right2text}</div> :
			<div class="row_S2v positive_zrK" style={centerStyle}>{right2text}</div>)}
	</div>
	);
};

// simple horizontal line
const DataDivider = () => {
	return (
	<div style={{display: 'flex', height: '4rem', flexDirection: 'column', justifyContent: 'center'}}>
		<div style={{borderBottom: '1px solid gray'}}></div>
	</div>
	);
};

// centered value, if flag exists then uses colors for negative/positive
// width is 10% by default
const SingleValue = ({ value, flag, width, small }) => {
	const rowClass = ( small ? "row_S2v small_ExK" : "row_S2v");
	const centerStyle = {
		width: width === undefined ? '10%' : width,
		justifyContent: 'center',
	};
	return (
		flag === undefined ? (
			<div class={rowClass}              style={centerStyle}>{value}</div>
		) : (
		flag ?
			<div class={rowClass + " negative_YWY"} style={centerStyle}>{value}</div> :
			<div class={rowClass + " positive_zrK"} style={centerStyle}>{value}</div>)
	);
};

const ResourceLine = ({ data }) => {
	return (
	<div class="labels_L7Q row_S2v" style={{lineHeight: 0.7}} >
		<div class="row_S2v" style={{width: '2%'}}></div>
		<div class="row_S2v" style={{width: '10%'}}>{data.resource}</div>
		<SingleValue value={data.demand}    width='5%' flag={data.demand<0} />
		<SingleValue value={data.building}  width='4%' flag={data.building<=0} />
		<SingleValue value={data.companies} width='4%' />
		<SingleValue value={data.workers}   width='5%' />
		<SingleValue value={data.svcfactor} width='4%' flag={data.svcfactor<0} small={true} />
		<SingleValue value={data.capfactor} width='4%' flag={data.capfactor<0} small={true} />
		<SingleValue value={data.wrkfactor} width='4%' flag={data.wrkfactor<0} small={true} />
		<SingleValue value={data.taxfactor} width='4%' flag={data.taxfactor<0} small={true} />
		<div class="row_S2v" style={{width: '54%', fontSize: '70%'}}>{data.details}</div>
	</div>
	);
};

const $Commercial = ({ react }) => {
	
	// demand data for each resource
	const [demandData, setDemandData] = react.useState([])
	useDataUpdate(react, 'realEco.commercialDemand', setDemandData)

	const onClose = () => {
		const data = { type: "toggle_visibility", id: 'realeco.commercial' };
		const event = new CustomEvent('hookui', { detail: data });
		window.dispatchEvent(event);
	};
	
	//const homelessThreshold = Math.round(residentialData[12] * residentialData[13] / 1000);

	return <$Panel react={react} title="Commercial Demand" onClose={onClose} initialSize={{ width: window.innerWidth * 0.55, height: window.innerHeight * 0.34 }} initialPosition={{ top: window.innerHeight * 0.05, left: window.innerWidth * 0.005 }}>
		{demandData.length === 0 ? (
			<p>Waiting...</p>
		) : (
		
		<div>
		
		<div class="labels_L7Q row_S2v">
			<div class="row_S2v" style={{width: '2%'}}></div>
			<div class="row_S2v" style={{width: '10%'}}>Resource</div>
			<SingleValue value="Demand"    width='5%' />
			<SingleValue value="Zone"  width='4%' />
			<SingleValue value="Num" width='4%' />
			<SingleValue value="Wrk"   width='5%' />
			<SingleValue value="Svc" width='4%' small={true} />
			<SingleValue value="Cap" width='4%' small={true} />
			<SingleValue value="Emp" width='4%' small={true} />
			<SingleValue value="Tax" width='4%' small={true} />
			<div class="row_S2v" style={{width: '54%'}}>Details</div>
		</div>
		
		{demandData
			.filter(item => item.resource !== 'NoResource')
			.map(item => ( <ResourceLine key={item.resource} data={item} />
		))}
		
		</div>
		
		)}
	</$Panel>
};

// Registering the panel with HookUI so it shows up in the menu
window._$hookui.registerPanel({
	id: "realeco.commercial",
	name: "RealEco: Commercial",
	icon: "Media/Game/Icons/ZoneCommercial.svg",
	component: $Commercial
});
