<template>
  <div>
    <h1>角色授权管理</h1>
    <el-table :data="userRoles" style="width: 100%">
      <el-table-column prop="UserId" label="用户ID" width="180"></el-table-column>
      <el-table-column prop="UserEntity.Mail" label="用户邮箱" width="180"></el-table-column>
      <el-table-column prop="RoleId" label="角色ID" width="180"></el-table-column>
      <el-table-column label="操作" width="180">
        <template slot-scope="scope" style="display: flex;">
          <el-button @click="editUserRole(userRoles.Id)" type="primary" size="small">编辑</el-button>
          <el-button @click="deleteUserRole(userRoles.Id)" type="danger" size="small">删除</el-button>
        </template>
      </el-table-column>
    </el-table>
    <el-button @click="showAddUserRoleDialog" type="primary" style="margin: 20px 0;">添加用户角色</el-button>

    <el-dialog title="添加用户角色" v-model="addUserRoleDialogVisible">
      <el-form :model="newUserRole">
        <el-form-item label="用户ID">
          <el-input v-model="newUserRole.UserId"></el-input>
        </el-form-item>
        <el-form-item label="角色ID">
          <el-input v-model="newUserRole.RoleId"></el-input>
        </el-form-item>
      </el-form>
      <div slot="footer" class="dialog-footer">
        <el-button @click="addUserRoleDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="addUserRole">确定</el-button>
      </div>
    </el-dialog>

    <el-dialog title="编辑用户角色" v-model="editUserRoleDialogVisible">
      <el-form :model="editUserRoleData">
        <el-form-item label="用户ID">
          <el-input v-model="editUserRoleData.UserId"></el-input>
        </el-form-item>
        <el-form-item label="角色ID">
          <el-input v-model="editUserRoleData.RoleId"></el-input>
        </el-form-item>
      </el-form>
      <div slot="footer" class="dialog-footer">
        <el-button @click="editUserRoleDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="updateUserRole">确定</el-button>
      </div>
    </el-dialog>
  </div>
</template>

<script>import $ from 'jquery';
import { ElMessage } from 'element-plus';

export default {
  data() {
    return {
      userRoles: [],
      addUserRoleDialogVisible: false,
      editUserRoleDialogVisible: false,
      newUserRole: {
        UserId: '',
        RoleId: ''
      },
      editUserRoleData: {
        Id: '',
        UserId: '',
        RoleId: ''
      }
    };
  },
  mounted() {
    this.getAllUserRoles();
  },
  methods: {
    getAllUserRoles() {
      $.ajax({
        url: 'https://modcat.top:8089/api/User/GetAllUserRole',
        type: 'POST',
        contentType: 'application/json',
        headers: {
          'Authorization': 'Bearer ' + localStorage.getItem('token')
        },
        data: JSON.stringify({ Skip: 0, Take: 100 }),
        success: (data) => {
          if (data.ResultData) {
            this.userRoles = data.ResultData;
          } else {
            ElMessage.error('获取用户角色失败: ' + data.ResultMsg);
          }
        },
        error: (err) => {
          ElMessage.error('获取用户角色失败: ' + err.responseJSON.ResultMsg);
        }
      });
    },
    showAddUserRoleDialog() {
      this.addUserRoleDialogVisible = true;
      console.log(this.addUserRoleDialogVisible);
    },
    addUserRole() {
      $.ajax({
        url: 'https://modcat.top:8089/api/User/AddUserRole',
        type: 'POST',
        contentType: 'application/json',
        headers: {
          'Authorization': 'Bearer ' + localStorage.getItem('token')
        },
        data: JSON.stringify(this.newUserRole),
        success: (data) => {
          if (data.ResultData) {
            ElMessage.success('添加用户角色成功');
            this.addUserRoleDialogVisible = false;
            this.getAllUserRoles();
          } else {
            ElMessage.error('添加用户角色失败: ' + data.ResultMsg);
          }
        },
        error: (err) => {
          ElMessage.error('添加用户角色失败: ' + err.responseJSON.ResultMsg);
        }
      });
    },
    editUserRole(row) {
      this.editUserRoleData = { ...row };
      this.editUserRoleDialogVisible = true;
    },
    updateUserRole() {
      $.ajax({
        url: 'https://modcat.top:8089/api/User/UpdateUserRole',
        type: 'POST',
        contentType: 'application/json',
        headers: {
          'Authorization': 'Bearer ' + localStorage.getItem('token')
        },
        data: JSON.stringify(this.editUserRoleData),
        success: (data) => {
          if (data.ResultData) {
            ElMessage.success('更新用户角色成功');
            this.editUserRoleDialogVisible = false;
            this.getAllUserRoles();
          } else {
            ElMessage.error('更新用户角色失败: ' + data.ResultMsg);
          }
        },
        error: (err) => {
          ElMessage.error('更新用户角色失败: ' + err.responseJSON.ResultMsg);
        }
      });
    },
    deleteUserRole(Id) {
      $.ajax({
        url: 'https://modcat.top:8089/api/User/DeleteUserRole',
        type: 'POST',
        contentType: 'application/json',
        headers: {
          'Authorization': 'Bearer ' + localStorage.getItem('token')
        },
        data: JSON.stringify({ Id: Id }),
        success: (data) => {
          if (data.ResultData) {
            ElMessage.success('删除用户角色成功');
            this.getAllUserRoles();
          } else {
            ElMessage.error('删除用户角色失败: ' + data.ResultMsg);
          }
        },
        error: (err) => {
          ElMessage.error('删除用户角色失败: ' + err.responseJSON.ResultMsg);
        }
      });
    }
  }
};</script>

<style scoped>
.dialog-footer {
  text-align: right;
}
</style>