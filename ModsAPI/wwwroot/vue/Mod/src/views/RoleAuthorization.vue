<template>
  <div>
    <h1>角色授权管理</h1>
    <el-table :data="userRoles" style="width: 100%">
      <el-table-column prop="UserEntity.Mail" label="用户邮箱" width="180"></el-table-column>
      <el-table-column prop="RoleEntity.RoleName" label="角色" width="180"></el-table-column>
      <el-table-column label="操作" width="180">
        <template v-slot="scope" style="display: flex;">
          <!-- <el-button @click="editUserRole(userRoles.Id)" type="primary" size="small">编辑</el-button> -->
          <el-button @click="deleteUserRole(scope.row.Id)" type="danger" size="small">删除</el-button>
        </template>
      </el-table-column>
    </el-table>
    <el-button @click="showAddUserRoleDialog" type="primary" style="margin: 20px 0;">添加用户角色</el-button>

    <el-dialog title="添加用户角色" v-model="addUserRoleDialogVisible">
      <el-form :model="newUserRole">
        <el-form-item label="用户邮箱">
          <el-input v-model="newUserRole.UserId"></el-input>
        </el-form-item>
        <el-form-item label="角色">
          <el-select v-model="newUserRole.RoleId" placeholder="请选择角色">
            <el-option v-for="role in Roles" :key="role.Id" :label="role.RoleName" :value="role.Id"></el-option>
          </el-select>
        </el-form-item>
      </el-form>
      <div slot="footer" class="dialog-footer">
        <el-button @click="addUserRoleDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="addUserRole">确定</el-button>
      </div>
    </el-dialog>

    <el-dialog title="编辑用户角色" v-model="editUserRoleDialogVisible">
      <el-form :model="editUserRoleData">
        <el-form-item label="用户邮箱">
          <el-input v-model="editUserRoleData.UserId"></el-input>
        </el-form-item>
        <el-form-item label="角色">
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
      Roles: [
        { "Id": "45166589-67eb-4012-abcc-817a0fa12c0e", "RoleName": "Developer" },
        { "Id": "b156c735-fe7b-421a-4764-78867798ef42", "RoleName": "Auditors" },
        { "Id": "74c3d1d8-d156-4314-bfea-a3162c014117", "RoleName": "ModDev" }
      ]
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
          'Authorization': 'Bearer ' + localStorage.getItem('token' + localStorage.getItem('Mail'))
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
          'Authorization': 'Bearer ' + localStorage.getItem('token' + localStorage.getItem('Mail'))
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
          'Authorization': 'Bearer ' + localStorage.getItem('token' + localStorage.getItem('Mail'))
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
      console.log(Id);
      if (Id === undefined || Id === null || Id === '') { return; }
      $.ajax({
        url: 'https://modcat.top:8089/api/User/DeleteUserRole',
        type: 'POST',
        contentType: 'application/json',
        headers: {
          'Authorization': 'Bearer ' + localStorage.getItem('token' + localStorage.getItem('Mail'))
        },
        data: "{ \"Id\": \"" + Id + "\" }",
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
body {
  background-image: url("https://www.loliapi.com/acg/pc/") !important;
  background-repeat: no-repeat;
  background-size: cover;
  background-position: center center;
  background-attachment: fixed;
}

.dialog-footer {
  text-align: right;
}
</style>