<template>
  <div>
    <el-card>
      <div style="margin-bottom: 16px;">
        <el-date-picker v-model="dateRange" type="daterange" range-separator="至" start-placeholder="开始日期"
          end-placeholder="结束日期" format="YYYY-MM-DD" value-format="YYYY-MM-DD" @change="fetchData" />
        <el-button type="primary" @click="fetchData" style="margin-left: 8px;">查询</el-button>
      </div>
      <div v-if="loading" style="text-align:center;">
        <el-loading />
      </div>
      <div v-else>
        <v-chart :option="chartOptionLogin" style="height: 400px;" />
        <el-table :data="tableDataLogin" style="margin-top: 24px;">
          <el-table-column prop="date" label="日期" />
          <el-table-column prop="count" label="当日活跃用户数量" />
        </el-table>
        <div style="margin-top:40px;"></div>
        <v-chart :option="chartOptionLost" style="height: 400px;" />
        <el-table :data="tableDataLost" style="margin-top: 24px;">
          <el-table-column prop="date" label="日期" />
          <el-table-column prop="count" label="流失用户数量" />
        </el-table>
        <div style="margin-top:40px;"></div>
        <div style="margin-bottom: 16px;">
          <el-date-picker v-model="apiDateRange" type="daterange" range-separator="至" start-placeholder="开始日期"
            end-placeholder="结束日期" format="YYYY-MM-DD" value-format="YYYY-MM-DD" @change="fetchApiData" />
          <el-button type="primary" @click="fetchApiData" style="margin-left: 8px;">查询接口请求</el-button>
        </div>
        <v-chart :option="chartOptionApi" style="height: 400px;" />
        <el-table :data="tableDataApi" style="margin-top: 24px;">
          <el-table-column prop="api" label="接口" />
          <el-table-column prop="count" label="请求次数" />
        </el-table>
      </div>
    </el-card>
  </div>
</template>

<script>
import { CanvasRenderer } from 'echarts/renderers'
import { LineChart, BarChart } from 'echarts/charts'
import { TooltipComponent, GridComponent } from 'echarts/components'
import VChart from 'vue-echarts'
import { use } from 'echarts/core'
import { ElMessage } from 'element-plus'

use([CanvasRenderer, LineChart, BarChart, TooltipComponent, GridComponent])

function getChartOption(isDark, name, color) {
  return {
    backgroundColor: isDark ? '#232323' : '#fff',
    tooltip: {
      trigger: 'axis',
      backgroundColor: isDark ? '#232323' : '#fff',
      borderColor: isDark ? '#121212' : 'rgb(255, 255, 255)',
      borderWidth: 2,
      textStyle: { color: isDark ? '#fff' : '#333', fontWeight: 'bold' },
      extraCssText: isDark ? 'box-shadow: 0 2px 8px #121212; border-radius: 8px;' : ''
    },
    xAxis: {
      type: 'category',
      data: [],
      axisLine: { lineStyle: { color: isDark ? '#bbb' : '#333' } },
      axisLabel: { color: isDark ? '#bbb' : '#333' },
      splitLine: { lineStyle: { color: isDark ? '#444' : '#eee' } }
    },
    yAxis: {
      type: 'value',
      axisLine: { lineStyle: { color: isDark ? '#bbb' : '#333' } },
      axisLabel: { color: isDark ? '#bbb' : '#333' },
      splitLine: { lineStyle: { color: isDark ? '#444' : '#eee' } }
    },
    series: [{
      type: 'line',
      data: [],
      name,
      color: color
    }]
  }
}

export default {
  components: { VChart },
  data() {
    return {
      dateRange: [],
      apiDateRange: [], // 新增
      loading: false,
      tableDataLogin: [],
      chartOptionLogin: getChartOption(false, '登录数量', '#4fc3f7'),
      tableDataLost: [],
      chartOptionLost: getChartOption(false, '流失用户数量', '#e74c3c'),
      tableDataApi: [],
      chartOptionApi: {
        backgroundColor: '#fff',
        tooltip: { trigger: 'axis' },
        xAxis: { type: 'category', data: [], axisLabel: { rotate: 30 } },
        yAxis: { type: 'value' },
        series: [{
          type: 'bar',
          data: [],
          name: '请求次数',
          color: '#81c784'
        }]
      }
    }
  },
  methods: {
    async fetchData() {
      this.loading = true
      let params = {}
      if (this.dateRange && this.dateRange.length === 2) {
        params = { start: this.dateRange[0], end: this.dateRange[1], days: this.getDays() }
      } else {
        params = { days: 7 }
      }
      const token = localStorage.getItem('token' + localStorage.getItem('Mail'))

      // 判断当前是否黑暗模式
      const isDark = document.body.classList.contains('dark-theme')
      this.chartOptionLogin = getChartOption(isDark, '登录数量', '#4fc3f7')
      this.chartOptionLost = getChartOption(isDark, '流失用户数量', '#e74c3c')

      // 登录统计
      const loginPromise = this.$axios({
        url: `${import.meta.env.VITE_API_BASE_URL}/Statistics/GetDailyLoginCount`,
        method: 'POST',
        data: { start: params.start, end: params.end },
        contentType: "application/json; charset=utf-8",
        responseType: 'json',
        headers: { 'Authorization': 'Bearer ' + token }
      });

      // 流失用户统计
      const lostPromise = this.$axios({
        url: `${import.meta.env.VITE_API_BASE_URL}/Statistics/GetDailyLostUserCount`,
        method: 'POST',
        data: { days: params.days },
        contentType: "application/json; charset=utf-8",
        responseType: 'json',
        headers: { 'Authorization': 'Bearer ' + token }
      });

      try {
        const [loginRes, lostRes] = await Promise.all([loginPromise, lostPromise]);

        // 登录统计
        if (loginRes.data && loginRes.data.ResultCode === 200) {
          const dict = loginRes.data.ResultData || {}
          const dates = Object.keys(dict)
          const counts = Object.values(dict)
          this.chartOptionLogin.xAxis.data = dates
          this.chartOptionLogin.series[0].data = counts
          this.tableDataLogin = dates.map((date, idx) => ({
            date,
            count: counts[idx]
          }))
        } else {
          ElMessage.error(loginRes.data?.ResultMsg || '获取登录数据失败')
        }

        // 流失用户统计
        if (lostRes.data && lostRes.data.ResultCode === 200) {
          const dict = lostRes.data.ResultData || {}
          const dates = Object.keys(dict)
          const counts = Object.values(dict)
          this.chartOptionLost.xAxis.data = dates
          this.chartOptionLost.series[0].data = counts
          this.tableDataLost = dates.map((date, idx) => ({
            date,
            count: counts[idx]
          }))
        } else {
          ElMessage.error(lostRes.data?.ResultMsg || '获取流失数据失败')
        }
      } catch (error) {
        ElMessage.error('请求失败: ' + (error.response?.data?.ResultMsg || error.message));
        console.log(error);
      } finally {
        this.loading = false
      }
    },
    async fetchApiData() {
      this.loading = true
      let params = {}
      if (this.apiDateRange && this.apiDateRange.length === 2) {
        params = {
          start: this.apiDateRange[0] + ' 00:00:00',
          end: this.apiDateRange[1] + ' 23:59:59'
        }
      } else {
        const today = new Date().toISOString().slice(0, 10)
        params = {
          start: today + ' 00:00:00',
          end: today + ' 23:59:59'
        }
      }
      const token = localStorage.getItem('token' + localStorage.getItem('Mail'))

      // 判断当前是否黑暗模式
      const isDark = document.body.classList.contains('dark-theme')

      try {
        const apiRes = await this.$axios({
          url: `${import.meta.env.VITE_API_BASE_URL}/Statistics/GetApiRequestCounts`,
          method: 'POST',
          data: { start: params.start, end: params.end },
          contentType: "application/json; charset=utf-8",
          responseType: 'json',
          headers: { 'Authorization': 'Bearer ' + token }
        });

        if (apiRes.data && apiRes.data.ResultCode === 200) {
          const dict = apiRes.data.ResultData || {}
          const apis = Object.keys(dict)
          const counts = Object.values(dict)
          // 直接整体替换 chartOptionApi，确保响应式
          this.chartOptionApi = {
            backgroundColor: isDark ? '#232323' : '#fff',
            tooltip: {
              trigger: 'axis',
              backgroundColor: isDark ? '#232323' : '#fff',
              borderColor: isDark ? '#444' : '#fff',
              textStyle: { color: isDark ? '#fff' : '#666' }
            },
            xAxis: {
              type: 'category',
              data: apis,
              axisLabel: { rotate: 30, color: isDark ? '#bbb' : '#333' },
              axisLine: { lineStyle: { color: isDark ? '#bbb' : '#333' } }
            },
            yAxis: {
              type: 'value',
              axisLine: { lineStyle: { color: isDark ? '#bbb' : '#333' } },
              axisLabel: { color: isDark ? '#bbb' : '#333' }
            },
            series: [{
              type: 'bar',
              data: counts,
              name: '请求次数',
              color: isDark ? '#aed581' : '#81c784'
            }]
          }
          this.tableDataApi = apis.map((api, idx) => ({
            api,
            count: counts[idx]
          }))
        } else {
          ElMessage.error(apiRes.data?.ResultMsg || '获取接口请求次数失败')
        }
      } catch (error) {
        ElMessage.error('请求失败: ' + (error.response?.data?.ResultMsg || error.message));
        console.log(error);
      } finally {
        this.loading = false
      }
    },
    detectDarkMode() {
      const isDarkMode = window.matchMedia('(prefers-color-scheme: dark)').matches;
      if (isDarkMode) {
        document.body.classList.add('dark-theme');
      } else {
        document.body.classList.remove('dark-theme');
      }
      window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
        if (e.matches) {
          document.body.classList.add('dark-theme');
        } else {
          document.body.classList.remove('dark-theme');
        }
        // 主题切换时刷新图表样式
        this.fetchData();
        this.fetchApiData();
      });
    },
    getDays() {
      if (this.dateRange && this.dateRange.length === 2) {
        const start = new Date(this.dateRange[0])
        const end = new Date(this.dateRange[1])
        return Math.min(30, Math.max(1, Math.ceil((end - start) / (1000 * 60 * 60 * 24)) + 1))
      }
      return 7
    }
  },
  mounted() {
    this.detectDarkMode();
    this.fetchData();
    this.fetchApiData();
  }
}
</script>

<style scoped>
.el-card {
  max-width: 800px;
  margin: 32px auto;
}
</style>

<style>
body {
  background-image: url("https://www.loliapi.com/acg/pc/") !important;
  background-repeat: no-repeat;
  background-size: cover;
  background-position: center center;
  background-attachment: fixed;
}

body.dark-theme {
  background-color: #121212;
  color: #ffffffa6;
}

body.dark-theme .el-card {
  background-color: #1e1e1e;
  color: #ffffffa6;
  border-color: #333333;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.5);
}

body.dark-theme .el-input__inner {
  background-color: #2c2c2c;
  color: #ffffffa6;
  border-color: #444444;
}

body.dark-theme .el-button {
  background-color: #333333;
  color: #ffffffa6;
  border-color: #444444;
}

body.dark-theme .el-button:hover {
  background-color: #444444;
  border-color: #555555;
}

body.dark-theme .el-tag {
  background-color: #2c2c2c;
  color: #ffffffa6;
  border-color: #444444;
}

body.dark-theme .line {
  background-color: #444444;
}

body.dark-theme .el-table {
  background-color: #1e1e1e;
  color: #ffffffa6;
  border-color: #333333;
}

body.dark-theme .el-table th,
body.dark-theme .el-table td {
  background-color: #1e1e1e;
  color: #ffffffa6;
  border-color: #333333;
}

body.dark-theme .el-table-column {
  color: #ffffffa6;
}

body.dark-theme .el-loading {
  color: #ffffffa6;
}

body.dark-theme h3,
body.dark-theme h2 {
  color: #ffffffa6;
}

body.dark-theme .el-date-picker,
body.dark-theme .el-picker-panel,
body.dark-theme .el-date-range-picker,
body.dark-theme .el-picker-panel__body,
body.dark-theme .el-picker-panel__content,
body.dark-theme .el-picker-panel__footer {
  background-color: #232323 !important;
  color: #ffffffa6 !important;
  border-color: #444444 !important;
}

body.dark-theme .el-date-picker .el-input__inner {
  background-color: #2c2c2c !important;
  color: #ffffffa6 !important;
  border-color: #444444 !important;
}

body.dark-theme .el-picker-panel__icon,
body.dark-theme .el-date-table th,
body.dark-theme .el-date-table td {
  color: #ffffffa6 !important;
}

body.dark-theme .el-date-table td.in-range,
body.dark-theme .el-date-table td.in-range:hover {
  background-color: #333333 !important;
}

body.dark-theme .el-date-table td.current,
body.dark-theme .el-date-table td.today {
  color: #fff !important;
}

body.dark-theme .el-picker-panel__btn {
  background-color: #333333 !important;
  color: #ffffffa6 !important;
  border-color: #444444 !important;
}

body.dark-theme .el-picker-panel__btn:hover {
  background-color: #444444 !important;
  border-color: #555555 !important;
}

body.dark-theme .el-date-editor,
body.dark-theme .el-date-editor .el-input__inner {
  background-color: #232323 !important;
  color: #ffffffa6 !important;
  border-color: #444444 !important;
}

body.dark-theme .el-date-editor .el-input__inner::placeholder {
  color: #bbbbbb !important;
  opacity: 1;
}

body.dark-theme .el-date-editor .el-range-separator {
  color: #bbbbbb !important;
}

body.dark-theme .el-date-editor .el-icon {
  color: #bbbbbb !important;
}

body.dark-theme .el-table__body tr:hover>td,
body.dark-theme .el-table__body tr.hover-row>td {
  background-color: #232323 !important;
  color: #fff !important;
  transition: background 0.2s;
}
</style>