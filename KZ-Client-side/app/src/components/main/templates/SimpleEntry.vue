<template>
	<v-app>
		<v-app-bar
			app
			height="54"
			elevation="0"
			:style="{backgroundImage: 'url(' + require('@/assets/kaunas.png') + ')'}"
		>
			<v-toolbar-title
				class="body-2 text-uppercase font-weight-medium"
			>
				{{$store.state.title}}
			</v-toolbar-title>
			<v-spacer></v-spacer>
			<slot name="header-components"></slot>
			<UserInfoContainer />
		</v-app-bar>
		<v-main>
			<slot></slot>
		</v-main>
		<CommonComponents />
	</v-app>
</template>

<script>
	import CommonComponents from "../../CommonComponents";
	import UserInfoContainer from "../../UserInfoContainer";

	export default {
		components: {
			CommonComponents,
			UserInfoContainer
		}
	};
</script>

<style scoped>
	.v-app-bar {
		background-color: white !important; /* #4b585f */
		border-bottom: 1px solid #efefef;
		background-position: left 10px center !important;
		background-size: auto 38px !important;
		image-rendering: -webkit-optimize-contrast;
	}
	.v-main {
		height: 100%;
	}
	.v-toolbar .v-toolbar__title {
		font-size: 0.95rem !important;
		padding-left: 35px;
	}
</style>