<script setup lang="ts">
import Timeline from "@/components/shared/Timeline.vue";
import Content from "@/components/layout/Content.vue";
import persona from "@/assets/persona.json";
</script>

<template>
  <Content id-ref="work">
    <Timeline :items="persona.experience" />
  </Content>
</template>
