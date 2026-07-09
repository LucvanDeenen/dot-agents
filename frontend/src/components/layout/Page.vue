<script setup lang="ts">
import { ChevronDoubleRightIcon } from "@heroicons/vue/24/outline";

defineProps<{
  title?: string;
}>();
</script>

<template>
  <section class="w-full">
    <!-- Header -->
    <div
      v-if="title"
      class="p-4 z-40 flex items-center sm:text-4xl text-xl mb-1 sm:mb-4 max-w-[1300px] mx-auto"
    >
      <ChevronDoubleRightIcon class="sm:w-10 w-10 mr-1 text-yellow-500" />
      <h2 class="section-title">
        {{ title }}
      </h2>
      <slot name="header"></slot>
    </div>

    <!-- Content -->
    <div :class="{ 'px-6 sm:text-base text-sm max-w-[1300px] mx-auto': title }">
      <slot></slot>
    </div>
  </section>
</template>
