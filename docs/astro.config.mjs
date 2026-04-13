// @ts-check
import {defineConfig} from 'astro/config';
import starlight from '@astrojs/starlight';

// https://astro.build/config
export default defineConfig({
    site: 'https://fuynaloft.github.io',
    base: '/MinionLib',
    integrations: [
        starlight({
            title: 'MinionLib Docs',
            defaultLocale: 'zh-cn',
            locales: {
                'zh-cn':{
                    label: '简体中文',
                    lang: 'zh-CN',
                },
                en:{
                    label: 'English',
                    lang: 'en',
                }
            },
            social: [{icon: 'github', label: 'GitHub', href: 'https://github.com/FuYnAloft/MinionLib'}],
        }),
    ],
});
