import { createApp } from 'vue'
import { createPinia } from 'pinia'
import './style.css'
import './assets/design-system.css'
import './assets/admin-theme.css'
import App from './App.vue'
import router from './router'

const app = createApp(App)

// Pinia must be installed before the router, since the router's
// navigation guard uses the auth store.
app.use(createPinia())
app.use(router)

app.mount('#app')
