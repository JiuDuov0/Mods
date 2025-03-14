import { createRouter, createWebHashHistory } from "vue-router";

const home = () => import('../views/Home.vue')
const login = () => import('../views/Login.vue')
const myCreateMods = () => import('../views/MyCreateMods.vue')
const mySubscribeMods = () => import('../views/MySubscribeMods.vue')
const register = () => import('../views/Register.vue')
const modDetail = () => import('../views/ModDetail.vue')

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
]

const router = createRouter({
    history: createWebHashHistory(),
    routes: routes,
})

export default router