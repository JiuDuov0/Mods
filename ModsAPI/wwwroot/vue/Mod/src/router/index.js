import { createRouter, createWebHashHistory } from "vue-router";

const home = import("../views/Home.vue")
const about = import("../views/About.vue")
const login = import("../views/Login.vue")

const routes = [
    { path: "/", name: "login", component: login },
    {
        path: "/about",
        name: "about",
        component: about
    },
    {
        path: "/home",
        name: "home",
        component: home
    },
]

const router = createRouter({
    history: createWebHashHistory(),
    routes: routes,
})

export default router