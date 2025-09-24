<template>
	<div :class="'ml-' + (level * 4)">
		<template v-for="(item, i) in legendItems">
			<div v-if="item.name" :key="i" :class="[i ? 'mt-2' : null, 'font-weight-medium mb-2']" >{{item.name}}</div>
			<template v-for="(legendItem, j) in item.legend">
				<div :key="i + '-' + j" :class="['d-flex align-center', j ? 'mt-2' : null]">
					<img
						:src="'data:' + legendItem.contentType + ';base64,' + legendItem.imageData"
						:width="legendItem.width"
						:height="legendItem.height"
					/>
					<span v-if="legendItem.label" class="ml-2">{{legendItem.label}}</span>
				</div>
			</template>
			<MapLayerLegendItem
				:legendItems="item.childrenLegends"
				:level="level + 1"
				:key="i + '-children'"
			/>
		</template>
	</div>
</template>

<script>
	export default {
		props: {
			legendItems: Array,
			level: Number
		},

		components: {
			MapLayerLegendItem: () => import("./MapLayerLegendItem.vue")
		}
	};
</script>