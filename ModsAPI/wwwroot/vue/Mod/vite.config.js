import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  server: {
    proxy: {
      '/bserver': {
        target: 'http://43.160.202.17:8099',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/bserver/, '')
      }
    }
  },
})
