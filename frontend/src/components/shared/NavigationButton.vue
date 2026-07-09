<script lang="ts" setup>
const props = defineProps<{
  left: boolean;
}>();

const emit = defineEmits<{
  (e: "scroll", direction: boolean): void;
}>();

function scroll(direction: boolean) {
  emit("scroll", direction);
}
</script>

<template>
  <button
    @click="scroll(!left)"
    class="overflow-button"
    :class="{
      '-left-2': props.left,
      '-right-2': !props.left,
    }"
  >
    {{ left ? "‹" : "›" }}
  </button>
</template>

<style lang="css" scoped>
.overflow-button {
  @apply absolute -translate-y-1/2 z-10 bg-neutral-800 text-white p-2 border rounded-full hidden sm:block top-1/2;
}

.overflow-button:hover {
  @apply text-yellow-500 border-yellow-500;
}
</style>
