<script setup lang="ts">
import Page from "@/components/layout/Page.vue";
import Education from "@/components/content/about/Education.vue";
import Skills from "@/components/content/about/Skills.vue";
import Bio from "@/components/content/about/Bio.vue";
</script>

<template>
  <Page title="About Me">
    <Bio />
    <Skills />
    <Education />
  </Page>
</template>
