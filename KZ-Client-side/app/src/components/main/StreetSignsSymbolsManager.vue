<template>
	<Entry>
		<template v-slot:header-components>
			<div
				class="d-flex mr-5"
			>
				<GoToMapButton />
			</div>
		</template>
		<div class="d-flex flex-column full-height">
			<div class="tabs pa-2">
				<v-btn-toggle
					v-model="tab"
					mandatory
					tile
					dense
					borderless
				>
					<v-btn
						small
						value="gallery"
						class="ma-0 mr-1"
						to="/sc/gallery"
						:disabled="Boolean((['/sc/edit', '/sc/element-edit'].indexOf($route.path) != -1) || ((['/sc/create', '/sc/element-create'].indexOf($route.path) != -1) && $route.query.type))"
					>
						Simbolių galerija
					</v-btn>
					<v-btn
						small
						value="elements-gallery"
						class="ma-0 mr-1"
						to="/sc/elements-gallery"
						:disabled="Boolean((['/sc/edit', '/sc/element-edit'].indexOf($route.path) != -1) || ((['/sc/create', '/sc/element-create'].indexOf($route.path) != -1) && $route.query.type))"
					>
						Simbolių elementų galerija
					</v-btn>
					<v-divider vertical class="ml-1 mr-2" />
					<v-btn
						small
						value="creator"
						class="ma-0 mr-1"
						to="/sc/create"
						:disabled="Boolean((['/sc/edit', '/sc/element-edit'].indexOf($route.path) != -1) || ((['/sc/element-create'].indexOf($route.path) != -1) && $route.query.type))"
					>
						Naujo simbolio kūrimas
					</v-btn>
					<v-btn
						small
						value="element-creator"
						class="ma-0 mr-1"
						to="/sc/element-create"
						:disabled="Boolean((['/sc/edit', '/sc/element-edit'].indexOf($route.path) != -1) || ((['/sc/create'].indexOf($route.path) != -1) && $route.query.type))"
					>
						Naujo simbolio elemento kūrimas
					</v-btn>
					<v-btn
						small
						value="creator"
						class="ma-0 mr-1"
						to="/sc/edit"
						v-if="$route.path == '/sc/edit'"
					>
						Simbolio redagavimas
					</v-btn>
					<v-btn
						small
						value="element-creator"
						class="ma-0 mr-1"
						to="/sc/element-edit"
						v-if="$route.path == '/sc/element-edit'"
					>
						Simbolio elemento redagavimas
					</v-btn>
				</v-btn-toggle>
			</div>
			<div class="pa-2 flex-grow-1 body-2 content">
				<keep-alive>
					<router-view></router-view> 
				</keep-alive>
			</div>
		</div>
		<StreetSignSymbolElementItemSelector />
		<StreetSignSymbolElementSettings />
		<ConfirmationDialog />
		<StreetSignSymbolInfoDialog />
	</Entry>
</template>

<script>
	import ConfirmationDialog from "../dialogs/ConfirmationDialog";
	import Entry from "./templates/SimpleEntry";
	import GoToMapButton from "../header-items/GoToMapButton";
	import StreetSignSymbolElementItemSelector from "../sc/StreetSignSymbolElementItemSelector";
	import StreetSignSymbolElementSettings from "../sc/StreetSignSymbolElementSettings";
	import StreetSignSymbolInfoDialog from "../sc/StreetSignSymbolInfoDialog";

	export default {
		data: function(){
			var data = {
				tab: null
			};
			return data;
		},

		components: {
			ConfirmationDialog,
			Entry,
			GoToMapButton,
			StreetSignSymbolElementItemSelector,
			StreetSignSymbolElementSettings,
			StreetSignSymbolInfoDialog
		}
	};
</script>

<style scoped>
	.tabs {
		border-bottom: 1px solid #efefef;
	}
	.content {
		overflow: auto;
		position: relative;
	}
	.v-btn--active {
		pointer-events: none;
	}
</style>