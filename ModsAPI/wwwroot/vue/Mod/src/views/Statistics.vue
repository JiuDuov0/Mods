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
          <el-table-column prop="count" label="登录数量" />
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
import * as echarts from 'echarts/core'
import { CanvasRenderer } from 'echarts/renderers'
import { LineChart } from 'echarts/charts'
import { TooltipComponent, GridComponent } from 'echarts/components'
import VChart from 'vue-echarts'
import { use } from 'echarts/core'
import { ElMessage } from 'element-plus'

use([CanvasRenderer, LineChart, TooltipComponent, GridComponent])

export default {
  components: { VChart },
  data() {
    return {
      dateRange: [],
      loading: false,
      // 登录统计
      tableDataLogin: [],
      chartOptionLogin: {
        tooltip: { trigger: 'axis' },
        xAxis: { type: 'category', data: [] },
        yAxis: { type: 'value' },
        series: [{ type: 'line', data: [], name: '登录数量' }]
      },
      // 流失用户统计
      tableDataLost: [],
      chartOptionLost: {
        tooltip: { trigger: 'axis' },
        xAxis: { type: 'category', data: [] },
        yAxis: { type: 'value' },
        series: [{ type: 'line', data: [], name: '流失用户数量', color: '#e74c3c' }]
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
    getDays() {
      if (this.dateRange && this.dateRange.length === 2) {
        const start = new Date(this.dateRange[0])
        const end = new Date(this.dateRange[1])
        // 计算天数，包含起止
        return Math.min(30, Math.max(1, Math.ceil((end - start) / (1000 * 60 * 60 * 24)) + 1))
      }
      return 7
    }
  },
  mounted() {
    this.fetchData()
  }
}
</script>

<style scoped>
.el-card {
  max-width: 800px;
  margin: 32px auto;
}
</style>