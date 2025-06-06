import { createRouter, createWebHashHistory } from "vue-router";

const home = () => import('../views/Home.vue')
const login = () => import('../views/Login.vue')
const myCreateMods = () => import('../views/MyCreateMods.vue')
const mySubscribeMods = () => import('../views/MySubscribeMods.vue')
const register = () => import('../views/Register.vue')
const modDetail = () => import('../views/ModDetail.vue')
const createMod = () => import('../views/CreateMod.vue')
const addVersionFile = () => import('../views/AddVersionFile.vue')
const addNewVersion = () => import('../views/AddNewVersion.vue')
const updateModInfo = () => import('../views/UpdateModInfo.vue')
const approveModVersion = () => import('../views/ApproveModVersion.vue')
const roleAuthorization = () => import('../views/RoleAuthorization.vue')

const routes = [
    { path: "/", name: "login", component: login },
    {
        path: "/register",
        name: "register",
        component: register
    },
    {
        path: "/home",
        name: "home",
        component: home
    },
    {
        path: "/myCreateMods",
        name: "myCreateMods",
        component: myCreateMods
    },
    {
        path: "/mySubscribeMods",
        name: "mySubscribeMods",
        component: mySubscribeMods
    },
    {
        path: "/modDetail",
        name: "modDetail",
        component: modDetail
    },
    {
        path: "/createMod",
        name: "createMod",
        component: createMod
    },
    {
        path: "/addVersionFile",
        name: "addVersionFile",
        component: addVersionFile
    },
    {
        path: "/addNewVersion",
        name: "addNewVersion",
        component: addNewVersion
    },
    {
        path: "/updateModInfo",
        name: "updateModInfo",
        component: updateModInfo
    },
    {
        path: "/approveModVersion",
        name: "approveModVersion",
        component: approveModVersion
    },
    {
        path: "/roleAuthorization",
        name: "roleAuthorization",
        component: roleAuthorization
    },
    {
        path: "/profile",
        name: "profile",
        component: () => import('../views/Profile.vue')
    },
    {
        path: "/myProfile",
        name: "myProfile",
        component: () => import('../views/MyProfile.vue')
    },
    {
        path: "/downloadmintcat",
        name: "downloadmintcat",
        component: () => import('../views/Downloadmintcat.vue')
    },
    {
        path: "/changePassword",
        name: "changePassword",
        component: () => import('../views/ChangePassword.vue')
    },
    {
        path: "/game",
        name: "game",
        component: () => import('../views/Game.vue')
    },
]

const router = createRouter({
    history: createWebHashHistory(),
    routes: routes,
    scrollBehavior(to, from, savedPosition) {
        if (to.hash) {
            return {
                el: to.hash,
                behavior: 'smooth',
            };
        }
        return { top: 0 };
    },
})

export default router