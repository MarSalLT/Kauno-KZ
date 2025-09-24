<template>
	<fragment>
		<TempMessage />
	</fragment>
</template>

<script>
	import {Fragment} from "vue-fragment";
	import TempMessage from "./TempMessage";

	export default {
		components: {
			Fragment,
			TempMessage
		}
	}
</script>

<style>
	body, html {
		height: 100% !important;
		overflow: hidden;
	}
	.v-application {
		height: 100% !important;
	}
	.v-application--wrap {
		min-height: 0 !important;
	}
	.plain.v-input {
		border: 1px solid rgba(234, 234, 234, 0.9);
		border-radius: 0 !important;
		background-color: white;
	}
	.plain.v-input--is-disabled {
		border-color: rgba(234, 234, 234, 0.2);
	}
	.v-text-field--outlined.v-input--is-focused fieldset, .v-text-field--outlined.v-input--has-state fieldset {
		border: 1px solid currentColor !important; /* Bibliotekoje yra 2px, bet labai negražiai atrodo... */
	}
	.full-height {
		height: 100% !important;
	}
	.v-menu__content {
		background-color: white;
	}
	.stop-event {
		pointer-events: none;
	}
	.stop-event > * {
		pointer-events: auto;
	}
	.stop-event > .stop-event {
		pointer-events: none;
	}
	.compact-form .v-input__slot, .compact-form .v-select__slot {
		min-height: auto !important;
	}
	.compact-form .v-input__slot {
		padding: 0 6px !important;
	}
	.compact-form .date-text-field .plain.v-input {
		min-width: auto;
		width: 140px;
	}
	.compact-form .date-text-field .time-field {
		width: 110px !important;
	}
	.wider-content.compact-form .th {
		width: 40% !important;
	}
	.v-select__selection {
		text-overflow: ellipsis;
		max-width: 170px !important;
	}
	.task-popup .v-select__selection {
		max-width: 350px !important;
	}
	.v-input .mdi-close { /* Select'uose panaikinimo mygtuką sumažiname */
		font-size: 18px !important;
	}
	.v-select-list {
		max-width: 400px;
	}
	.plain .v-input__append-inner {
		margin-top: 5px !important; /* Kad pasirinkimo rodyklytė ir trynimo mygtukas nebūtų per daug apačioje... */
	}
	.user-data-form .plain .v-input__append-inner {
		margin-top: 9px !important;
	}
	.svg-symbol {
		display: block;
		margin: 0 auto;
	}
	.ol-tooltip {
		position: relative;
		background: rgba(0, 0, 0, 0.5);
		border-radius: 4px;
		color: white;
		padding: 4px 8px;
		opacity: 0.7;
		white-space: nowrap;
		font-size: 12px;
	}
	.ol-tooltip-measure {
		opacity: 1;
		font-weight: bold;
	}
	.ol-tooltip-static {
		background-color: #ffcc33;
		color: black;
		border: 1px solid white;
	}
	.ol-tooltip-measure:before,
	.ol-tooltip-static:before {
		border-top: 6px solid rgba(0, 0, 0, 0.5);
		border-right: 6px solid transparent;
		border-left: 6px solid transparent;
		content: "";
		position: absolute;
		bottom: -6px;
		margin-left: -7px;
		left: 50%;
	}
	.ol-tooltip-static:before {
		border-top-color: #ffcc33;
	}
	.point-progress {
		border: 5px solid #f3f3f3;
		border-top: 5px solid #3498db;
		border-radius: 50%;
		width: 24px;
		height: 24px;
		animation: spin 0.7s linear infinite;
	}
	@keyframes spin {
		from {
			transform: rotate(0deg);
		}
		to {
			transform: rotate(360deg);
		}
	}
	.AttributionContainer {
		height: auto;
	}
	.psv-overlay-image svg {
		width: 40px !important;
	}
	.my-table, .simple-data-table table {
		border-collapse: separate;
		border-spacing: 0;
		border-top: 1px solid #e0e0e0;
		width: 100%; /* Norint įtikti Haroldui */
	}
	.my-table th, .my-table td, .simple-data-table th, .simple-data-table td {
		border: none;
		border-right: 1px solid #e0e0e0;
		border-bottom: 1px solid #e0e0e0;
	}
	.my-table th:first-child , .my-table td:first-child, .simple-data-table th:first-child, .simple-data-table td:first-child {
		border-left: 1px solid #e0e0e0;
	}
	.vertical-street-signs-selector table, .vertical-street-signs-selector th, .vertical-street-signs-selector td {
		border-color: #cccccc !important;
	}
	.my-table td {
		height: 35px;
	}
	.my-table th {
		text-align: left;
	}
	.my-table th.wide {
		width: 45%; /* Norint įtikti Haroldui */
	}
	.bolder-background {
		background-color: #f6f6f6;
	}
	.bordered-container {
		border: 1px solid #e0e0e0;
	}
	.clickable:hover {
		background-color: #d1efff !important;
	}
	.table-wrapper {
		overflow: auto;
		height: 100%;
	}
	.table-wrapper table, .users-manager table {
		width: auto !important;
	}
	.users-manager th, .users-manager td {
		height: 60px !important;
	}
	.overflow-y {
		overflow-x: hidden;
		overflow-y: auto;
	}
	/* Atributų lentelės redagavimas: */
	.popup .v-input__slot {
		min-height: auto !important;
	}
	.popup .v-text-field input {
		padding-top: 0 !important;
		padding-bottom: 0 !important;
	}
	.popup .v-select__slot {
		height: 26px;
	}
	.popup .date-text-field .time-field {
		width: 150px;
	}
	.popup .v-text-field .v-input__append-inner {
		margin-top: 0 !important;
	}
	.popup .v-text-field .v-input__icon--clear {
		margin-top: 3px;
	}
	.popup .v-select .v-input__icon--clear {
		margin-top: 4px;
	}
	.popup .date-text-field .v-input__icon--clear {
		margin-top: 2px;
	}
	.popup .v-text-field .v-input__icon {
		height: auto;
	}
	.popup .val-pretty, .popup .v-select__selection {
		letter-spacing: normal !important;
	}
	/* Naujo objekto kūrimo įrankis */
	.my-tabs .v-btn--active {
		background-color: white !important;
		border-bottom-color: white !important;
		pointer-events: none;
		z-index: 2 !important;
	}
	.my-tabs .v-btn--active:before {
		display: none !important;
	}
	/* ... */
	.search-content-table .v-data-table-header th {
		white-space: nowrap;
		height: auto !important;
		padding: 4px 8px !important;
	}
	.v-data-table-header th {
		background-color: #f6f6f6;
	}
	.help-dialog {
		width: 95%;
		height: 95%;
	}
	.sc-dialog {
		width: 100%;
		height: 100%;
	}
	.feature-photos-manager-dialog {
		width: auto !important;
		min-height: 200px;
	}
	.drop-zone {
		border-style: dashed !important;
		border-color: #d4d4d4 !important; /* Arba gal net koks $vuetify.theme.themes.light.primary */
	}
	.radio-group-row .v-input--radio-group__input {
		flex-direction: row !important;
		align-items: start;
	}
	#map-tooltip img {
		display: block;
		margin-top: 3px;
	}
	.link-button {
		height: auto !important;
	}
	.link-button .v-btn__content {
		max-width: 220px !important;
		white-space: normal !important;
	}
	.identify-progress {
		border: 5px solid #f3f3f3;
		border-top: 5px solid #3498db;
		border-radius: 50%;
		width: 24px;
		height: 24px;
		animation: spin 0.7s linear infinite;
	}
	@keyframes spin {
		from {
			transform: rotate(0deg);
		}
		to {
			transform: rotate(360deg);
		}
	}
	.black-spots-dialog {
		width: 99% !important;
		height: 99%;
		max-height: 100% !important;
		margin: 0 !important;
	}
	.compact-table th, .compact-table td {
		height: 35px !important;
	}
	.v-treeview--dense .v-treeview-node__root {
		min-height: 32px !important;
	}
	.v-treeview-node__label {
		white-space: normal !important;
	}
</style>