<template>
	<div class="date-text-field">
		<template v-if="withTime">
			<div class="d-flex align-center">
				<v-menu
					v-model="dateMenu"
					:close-on-content-click="false"
					transition="scale-transition"
					offset-y
					min-width="290px"
					max-width="290px"
				>
					<template v-slot:activator="{on, attrs}">
						<v-text-field
							v-model="date"
							dense
							hide-details
							:outlined="outlined"
							:id="id"
							:class="['body-2 ma-0', simple ? null : 'plain']"
							readonly
							v-bind="attrs"
							v-on="on"
							clearable
							:error="error"
							:disabled="!editable"
						>
							<template v-slot:append>
								<v-btn
									icon
									height="24"
									width="24"
									tabindex="-1"
									v-on="on"
									:disabled="!editable"
								>
									<v-icon title="Kalendorius" small>mdi-calendar</v-icon>
								</v-btn>
							</template>
						</v-text-field>
					</template>
					<v-date-picker
						v-model="date"
						no-title
						@input="dateMenu = false"
						:first-day-of-week="1"
					></v-date-picker>
				</v-menu>
				<v-menu
					v-model="timeMenu"
					:close-on-content-click="false"
					transition="scale-transition"
					offset-y
					min-width="290px"
					max-width="290px"
				>
					<template v-slot:activator="{on, attrs}">
						<v-text-field
							v-model="time"
							dense
							hide-details
							:outlined="outlined"
							:class="['body-2 ma-0 time-field', simple ? null : 'plain']"
							readonly
							v-bind="attrs"
							v-on="on"
							:clearable="time != '00:00'"
							:disabled="!editable || !date"
						>
							<template v-slot:append>
								<v-btn
									icon
									height="24"
									width="24"
									tabindex="-1"
									v-on="on"
									:disabled="!editable || !date"
								>
									<v-icon title="Laikrodis" small>mdi-clock-time-four-outline</v-icon>
								</v-btn>
							</template>
						</v-text-field>
					</template>
					<v-time-picker
						v-model="time"
						format="24hr"
					></v-time-picker>
				</v-menu>
			</div>
		</template>
	</div>
</template>

<script>
	export default {
		data: function(){
			var data = {
				dateMenu: false,
				timeMenu: false,
				date: null,
				time: null,
				outlined: !this.simple
			};
			var date;
			if (this.value) {
				var value = parseInt(this.value);
				// Turime laiką milisekundėmis... O ką mums `timeZoneData` duoda??...
				// BIG FIXME!... Testuoti sukuriant objektą ir kokiame nors field'e nurodant tą pačią minutę?.. Ir lyginti rezultatus?.. Laikas turėtų būti vienodas??? Ar ne?..
				// Ką tas `dateFieldsTimeReference` nustato? Ar jis aktualus, kai turime laiką milisekundėmis? Ar jis aktualus tik kitur, pvz. kai norime tas milisekundes pateikti kaip datą kažkur?..
				// Dabar jokių konvertacijų nedarome... Tiesiog pasitikime funkcija Date()...
				date = new Date(value);
				if (date) {
					data.date = date.getFullYear() + "-" + this.pad((date.getMonth() + 1), 2) + "-" + this.pad(date.getDate(), 2);
					data.time = this.pad(date.getHours(), 2) + ":" + this.pad(date.getMinutes(), 2);
				}
			}
			return data;
			// FIXME!
			// Yra toks nelabai malonus niuansas: https://github.com/vuetifyjs/vuetify/issues/4502
		},

		props: {
			value: String,
			id: String,
			withTime: Boolean,
			simple: Boolean,
			error: Boolean,
			editable: Boolean,
			timeZoneData: Object
		},

		computed: {
			inputVal: {
				get: function(){
					return this.value;
				},
				set: function(val){
					this.$emit("input", val);
				}
			}
		},

		methods: {
			setInputVal: function(){
				var inputVal = this.date;
				if (inputVal && this.time) {
					inputVal += " " + this.time;
				}
				if (inputVal) {
					inputVal = new Date(inputVal).getTime(); // FIXME! Čia gali tekti pakoreguoti laiką, atsižvelgiant į `dateFieldsTimeReference`? Ar ne?..
					inputVal += "";
				}
				this.inputVal = inputVal;
			},
			pad: function(n, width, z) {
				z = z || "0";
				n = n + "";
				return n.length >= width ? n : new Array(width - n.length + 1).join(z) + n;
			}
		},

		watch: {
			date: {
				immediate: false,
				handler: function(date){
					if (!date) {
						this.time = null;
					} else {
						if (!this.time) {
							this.time = "00:00";
						}
					}
					this.setInputVal();
				}
			},
			time: {
				immediate: true,
				handler: function(time){
					if (this.date && !time) {
						this.time = "00:00";
					}
					this.setInputVal();
				}
			},
			inputVal: {
				immediate: true,
				handler: function(inputVal){
					if (!inputVal) {
						this.date = this.time = null;
					}
				}
			}
		}
	}
</script>