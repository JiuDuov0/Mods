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
      </div>
    </el-card>
  </div>
</template>

<script>
import { CanvasRenderer } from 'echarts/renderers'
import { LineChart } from 'echarts/charts'
import { TooltipComponent, GridComponent } from 'echarts/components'
import VChart from 'vue-echarts'
import { use } from 'echarts/core'
import { ElMessage } from 'element-plus'

use([CanvasRenderer, LineChart, TooltipComponent, GridComponent])

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
      loading: false,
      tableDataLogin: [],
      chartOptionLogin: getChartOption(false, '登录数量', '#4fc3f7'),
      tableDataLost: [],
      chartOptionLost: getChartOption(false, '流失用户数量', '#e74c3c')
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