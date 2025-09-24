import Vue from "vue";
import App from "./App.vue";
import vuetify from "../plugins/vuetify";
import router from "./router.js";
import store from "./store.js";

Vue.prototype.$vBus = new Vue();

Vue.config.productionTip = false;

new Vue({
  vuetify,
  store,
  router,
  render: h => h(App)
}).$mount("#root");