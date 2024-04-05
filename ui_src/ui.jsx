import "./styles.css";
import $Commercial    from "./commercial";

// Gooee
const MyWindow = ({ react, setupController }) => {
    const { model, update, trigger, _L } = setupController();
    
	return (
	<div class="fixdiv">
    <div>{model.IsVisibleCommercial ? <$Commercial react={react} /> : null}</div>
	</div>
	);
};
window.$_gooee.register("realeco", "MyWindow", MyWindow, "main-container", "realeco");
