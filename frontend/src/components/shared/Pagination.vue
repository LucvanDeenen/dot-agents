<script lang="ts" setup>
const props = defineProps<{
  length: number;
  index: number;
}>();
</script>

<template>
  <div class="flex justify-center my-4">
    <div
      v-for="i in props.length"
      class="w-2 h-2 rounded-full shadow-md mx-2 transition-colors duration-300"
      :class="i - 1 === index ? 'bg-yellow-500' : 'bg-neutral-700'"
    ></div>
  </div>
</template>
