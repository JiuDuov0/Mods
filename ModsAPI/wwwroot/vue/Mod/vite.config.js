import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import Components from 'unplugin-vue-components/vite';
import { ElementPlusResolver } from 'unplugin-vue-components/resolvers';
import AutoImport from 'unplugin-auto-import/vite';

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    Components({
      resolvers: [ElementPlusResolver()],
    }),
    AutoImport({
      imports: ['vue', 'vue-router', '@vueuse/core'],
      dts: true,
      resolvers: [ElementPlusResolver()],
    }),
  ],
  base: process.env.NODE_ENV === 'production' ? '/' : '/'
})
