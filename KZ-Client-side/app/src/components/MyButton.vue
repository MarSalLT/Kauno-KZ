<template>
	<v-btn
		icon
		small
		v-on:click.stop.prevent="handleActionButtonClick(descr)"
		:loading="loading"
	>
		<v-icon
			:title="descr.title"
			:color="descr.color"
			:size="descr.size"
		>
			{{descr.icon}}
		</v-icon>
	</v-btn>
</template>

<script>
	export default {
		data: function(){
			var data = {
				loading: false
			};
			return data;
		},

		props: {
			descr: Object
		},

		methods: {
			handleActionButtonClick: function(e){
				if (e.onClick) {
					e.onClick(e.item, this);
				}
			}
		}
	};
</script>