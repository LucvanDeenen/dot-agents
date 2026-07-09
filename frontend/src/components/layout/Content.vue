<script setup lang="ts">
import { ref } from "vue";
const contentEl = ref<HTMLDivElement | null>(null);
defineExpose({
  contentEl,
});

defineProps<{
  override?: {
    type: boolean;
    default: false;
  };
  idRef: string;
}>();
</script>

<template>
  <div :class="{ 'mb-12': !override }" ref="contentEl" :id="idRef">
    <slot></slot>
  </div>
</template>
