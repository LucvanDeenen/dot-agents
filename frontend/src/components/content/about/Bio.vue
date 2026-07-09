<script setup lang="ts">
import Content from "@/components/layout/Content.vue";
import persona from "@/assets/persona.json";
</script>

<template>
  <Content id-ref="bio">
    <p class="mb-3 leading-relaxed">
      {{ persona.summary }}
    </p>
    <div class="mt-3">
      <h4>Areas of Interest</h4>
      <div class="flex flex-wrap gap-3">
        <span
          v-for="interest in persona.interests"
          :key="interest"
          class="chip"
        >
          {{ interest }}
        </span>
      </div>
    </div>
  </Content>
</template>
