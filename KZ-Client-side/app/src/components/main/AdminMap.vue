<template>
	<Entry>
		<template v-slot:header-components>
			<div
				class="d-flex mr-3"
				v-if="canViewButtons && myMap"
			>
				<HelpButton class="mr-7" />
				<UsersManagerButton
					class="mr-7"
					v-if="canManageUsers"
				/>
				<StreetSignsSymbolsManagerButton
					class="mr-7"
					v-if="canManageStreetSignsSymbols"
				/>
				<TasksBadge
					class="mr-7"
					v-if="canHandleTasks"
				/>
				<UnapprovedStreetSignsBadge
					class="mr-7"
					v-if="canApproveStreetSigns"
				/>
				<RejectedStreetSignsBadge
					class="mr-7"
					v-if="canSeeRejectedStreetSigns"
				/>
			</div>
		</template>
		<div class="d-flex flex-column full-height">
			<StreetSignsMap></StreetSignsMap>
		</div>
	</Entry>
</template>

<script>
	import Entry from "./templates/SimpleEntry";
	import HelpButton from "../header-items/HelpButton";
	import RejectedStreetSignsBadge from "../header-items/RejectedStreetSignsBadge";
	import StreetSignsSymbolsManagerButton from "../header-items/StreetSignsSymbolsManagerButton";
	import StreetSignsMap from "../StreetSignsMap";
	import TasksBadge from "../header-items/TasksBadge";
	import UnapprovedStreetSignsBadge from "../header-items/UnapprovedStreetSignsBadge";
	import UsersManagerButton from "../header-items/UsersManagerButton";

	export default {
		data: function(){
			var data = {
				canViewButtons: false,
				canManageUsers: false,
				canManageStreetSignsSymbols: false,
				canApproveStreetSigns: false,
				canSeeRejectedStreetSigns: false,
				canHandleTasks: false
			};
			return data;
		},

		components: {
			Entry,
			HelpButton,
			RejectedStreetSignsBadge,
			StreetSignsSymbolsManagerButton,
			StreetSignsMap,
			TasksBadge,
			UnapprovedStreetSignsBadge,
			UsersManagerButton
		},

		computed: {
			myMap: {
				get: function(){
					return this.$store.state.myMap;
				}
			},
			userData: {
				get: function(){
					return this.$store.state.userData;
				}
			}
		},

		watch: {
			userData: {
				immediate: true,
				handler: function(userData){
					if (userData) {
						if (userData.permissions && userData.permissions.length) {
							userData.permissions.forEach(function(permission){
								if (permission == "manage-users") {
									this.canManageUsers = true;
								} else if (permission == "approve") {
									this.canApproveStreetSigns = true;
								} else if (permission == "sc") {
									this.canManageStreetSignsSymbols = true;
								} else if (permission == "kz-vertical-edit") {
									this.canSeeRejectedStreetSigns = true;
								} else if ((permission == "manage-tasks") || (permission == "manage-tasks-test") || (permission == "kz-edit")) {
									// TODO... išplėsti?..
									this.canHandleTasks = true;
								}
							}.bind(this));
							this.canViewButtons = true;
						}
					}
				}
			}
		}
	};
</script>