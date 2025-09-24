<template>
	<div>
		<template v-if="['117', '118', '304', '318', '319', '529', '530', '531', '546'].indexOf(data.type) != -1">
			<StreetSignSymbolContentUsingBackgroundImage
				:data="data"
				ref="dataContent"
			/>
		</template>
		<template v-else-if="['314', '329'].indexOf(data.type) != -1">
			<StreetSignSymbolContentText
				:data="data"
				ref="dataContent"
				shape="circle"
				:outlineColor="colors.red"
				:outlineWidth="4"
				:emptyText="data.type == '314' ? 'XXt' : null"
			/>
		</template>
		<template v-else-if="data.type == '315'">
			<StreetSignSymbolContentText
				:data="data"
				ref="dataContent"
				shape="circle"
				:outlineColor="colors.red"
				:outlineWidth="4"
				emptyText="XXt"
				:specificParams="{below: 'wheels'}"
			/>
		</template>
		<template v-else-if="['316', '317'].indexOf(data.type) != -1">
			<StreetSignSymbolContentText
				:data="data"
				ref="dataContent"
				shape="circle"
				:outlineColor="colors.red"
				:outlineWidth="4"
				:emptyText="data.type == '316' ? 'X,Xm' : 'X,X'"
				:specificParams="{triangles: data.type == '316' ? 'vertical' : 'horizontal'}"
			/>
		</template>
		<template v-else-if="data.type == '330'">
			<StreetSignSymbolContentText
				:data="data"
				ref="dataContent"
				shape="circle"
				:textColor="colors.grey"
				outlineColor="#555555"
				:outlineWidth="2"
				:specificParams="{diagonalLines: true}"
			/>
		</template>
		<template v-else-if="sc5XXContent.indexOf(data.type) != -1">
			<template v-if="sc5XXInteractive">
				<StreetSignSymbolContent5XXInteractive
					:data="data"
					:maxElementsLength="['507', '513', '514', '515', '516', '517', '518', '520', '521', '522', '523', '524'].indexOf(data.type) != -1 ? 6 : 1"
					ref="dataContent"
				/>
			</template>
			<template v-else>
				<StreetSignSymbolContent5XX
					:data="data"
					:maxElementsLength="['507', '513', '514', '515', '516', '517', '518', '520', '521', '522', '523', '524'].indexOf(data.type) != -1 ? 6 : 1"
					ref="dataContent"
				/>
			</template>
		</template>
		<template v-else-if="['540', '541', '542', '543', '545'].indexOf(data.type) != -1">
			<StreetSignSymbolContentZone
				:data="data"
				ref="dataContent"
				:subtype="data.type"
			/>
		</template>
		<template v-else-if="['604'].indexOf(data.type) != -1">
			<StreetSignSymbolContentMultipleDivs
				:data="data"
				:advanced="true"
				ref="dataContent"
			/>
		</template>
		<template v-else-if="data.type == '616'">
			<StreetSignSymbolContentText
				:data="data"
				ref="dataContent"
				shape="rectangle"
				belowElement="any-side-arrow"
			/>
		</template>
		<template v-else-if="['622', '623'].indexOf(data.type) != -1">
			<StreetSignSymbolContentMultipleDivs
				:data="data"
				ref="dataContent"
			/>
		</template>
		<template v-else-if="['617', '824', '825', '826', '828'].indexOf(data.type) != -1">
			<StreetSignSymbolContentText
				:data="data"
				ref="dataContent"
				shape="rectangle"
			/>
		</template>
		<template v-else-if="['612', '615', '620'].indexOf(data.type) != -1">
			<StreetSignSymbolContentText
				:data="data"
				ref="dataContent"
				shape="rectangle"
				:fillColor="colors.blue"
				textColor="white"
				outlineColor="white"
				:emptyText="data.type == '620' ? 'XX' : null"
			/>
		</template>
		<template v-else-if="data.type == '801'">
			<StreetSignSymbolContentText
				:data="data"
				ref="dataContent"
				shape="rectangle"
				:xPaddingMultiplyBy="2"
			/>
		</template>
		<template v-else-if="data.type == '802'">
			<StreetSignSymbolContentText
				:data="data"
				ref="dataContent"
				shape="rectangle"
				rightElement="arrow-right"
			/>
		</template>
		<template v-else-if="data.type == '803'">
			<StreetSignSymbolContentText
				:data="data"
				ref="dataContent"
				shape="rectangle"
				leftElement="arrow-left"
			/>
		</template>
		<template v-else-if="data.type == '804'">
			<StreetSignSymbolContentText
				:data="data"
				ref="dataContent"
				shape="rectangle"
				aboveElement="STOP"
			/>
		</template>
		<template v-else-if="data.type == '805'">
			<StreetSignSymbolContentText
				:data="data"
				ref="dataContent"
				shape="rectangle"
				leftElement="arrow-up"
				rightElement="arrow-up"
			/>
		</template>
		<template v-else-if="data.type == '806'">
			<StreetSignSymbolContentText
				:data="data"
				ref="dataContent"
				shape="rectangle"
				aboveElement="arrow-up"
				belowElement="m"
			/>
		</template>
		<template v-else-if="data.type == '809'">
			<StreetSignSymbolContentText
				:data="data"
				ref="dataContent"
				shape="rectangle"
				belowElement="arrow-right"
			/>
		</template>
		<template v-else-if="data.type == '810'">
			<StreetSignSymbolContentText
				:data="data"
				ref="dataContent"
				shape="rectangle"
				belowElement="arrow-left"
			/>
		</template>
		<template v-else-if="data.type == '811'">
			<StreetSignSymbolContentText
				:data="data"
				ref="dataContent"
				shape="rectangle"
				belowElement="arrow-both"
			/>
		</template>
		<template v-else-if="data.type == '829'">
			<StreetSignSymbolContentMultipleDivs
				:data="data"
				ref="dataContent"
			/>
		</template>
		<template v-else-if="['842', '843'].indexOf(data.type) != -1">
			<StreetSignSymbolContent842And843
				:data="data"
				ref="dataContent"
			/>
		</template>
		<template v-else-if="['413', '611', '830', '831', '832', '832', '833', '834', '835', '836', '837', '838'].indexOf(data.type) != -1">
			<StreetSignSymbolContentProperImagePicker
				:data="data"
				ref="dataContent"
			/>
		</template>
		<template v-else-if="['840', '921', '922', '923', '924'].indexOf(data.type) != -1">
			<StreetSignSymbolContentMultipleDivs
				:data="data"
				ref="dataContent"
			/>
		</template>
		<template v-else-if="data.type == '920'">
			<StreetSignSymbolContent920
				:data="data"
				ref="dataContent"
			/>
		</template>
		<template v-else>
			...
		</template>
	</div>
</template>

<script>
	import CommonHelper from "../../helpers/CommonHelper";
	import StreetSignSymbolContentMultipleDivs from "./StreetSignSymbolContentMultipleDivs";
	import StreetSignSymbolContentProperImagePicker from "./StreetSignSymbolContentProperImagePicker";
	import StreetSignSymbolContentText from "./StreetSignSymbolContentText";
	import StreetSignSymbolContentUsingBackgroundImage from "./StreetSignSymbolContentUsingBackgroundImage";
	import StreetSignSymbolContentZone from "./StreetSignSymbolContentZone";
	import StreetSignSymbolContent5XX from "./StreetSignSymbolContent5XX";
	import StreetSignSymbolContent5XXInteractive from "./StreetSignSymbolContent5XXInteractive";
	import StreetSignSymbolContent842And843 from "./StreetSignSymbolContent842And843";
	import StreetSignSymbolContent920 from "./StreetSignSymbolContent920";

	export default {
		data: function(){
			var data = {
				colors: CommonHelper.colors.sc || {},
				sc5XXInteractive: this.$store.state.sc5XXInteractive,
				sc5XXContent: CommonHelper.sc5XXContent || []
			};
			return data;
		},

		props: {
			data: Object
		},

		components: {
			StreetSignSymbolContentMultipleDivs,
			StreetSignSymbolContentProperImagePicker,
			StreetSignSymbolContentText,
			StreetSignSymbolContentUsingBackgroundImage,
			StreetSignSymbolContentZone,
			StreetSignSymbolContent5XX,
			StreetSignSymbolContent5XXInteractive,
			StreetSignSymbolContent842And843,
			StreetSignSymbolContent920
		},

		methods: {
			getAllData: function(){
				var data;
				if (this.$refs.dataContent && this.$refs.dataContent.getAllData) {
					data = this.$refs.dataContent.getAllData();
				}
				return data;
			}
		}
	};
</script>