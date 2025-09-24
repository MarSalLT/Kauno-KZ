<template>
	<v-app
		:style="{backgroundImage: 'url(' + require('@/assets/main.jpg') + ')'}"
	>
		<v-app-bar
			app
			height="75"
			elevation="0"
			:style="{backgroundImage: 'url(' + require('@/assets/kaunas.png') + ')'}"
		>
			<v-toolbar-title
				:class="['title', 'text-uppercase', breakpointName == 'xs' ? 'body-1 text-wrap' : null]"
			>
				{{$store.state.title}}
			</v-toolbar-title>
			<v-spacer></v-spacer>
			<UserInfoContainer />
		</v-app-bar>
		<v-main>
			<v-container fill-height fluid>
				<v-row>
					<v-col class="d-flex justify-center">
						<div
							class="main-component py-10 px-5 elevation-2"
							:style="{width: mainComponentWidth ? mainComponentWidth + 'px' : null}"
						>
							<slot></slot>
						</div>
					</v-col>
				</v-row>
			</v-container>
		</v-main>
		<CommonComponents />
	</v-app>
</template>

<script>
	import CommonComponents from "../../CommonComponents";
	import UserInfoContainer from "../../UserInfoContainer";

	export default {
		computed: {
			breakpointName: {
				get: function(){
					return this.$vuetify.breakpoint.name;
				}
			}
		},

		props: {
			mainComponentWidth: Number
		},

		components: {
			CommonComponents,
			UserInfoContainer
		}
	};
</script>

<style scoped>
	.v-application {
		background-size: cover !important;
		background-position: center center !important;
	}
	.v-app-bar {
		background-color: rgba(255, 255, 255, 0.95) !important;
		background-position: left 12px center !important;
	}
	.v-main {
		height: 100%;
		overflow: auto;
		background-color: rgba(255, 255, 255, 0.20);
	}
	.title {
		padding-left: 55px;
	}
	.main-component {
		background-color: rgba(255, 255, 255, 0.98);
		max-width: 700px;
		width: 900px;
	}
</style>