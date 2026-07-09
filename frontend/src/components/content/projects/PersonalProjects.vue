<script setup lang="ts">
import { ref } from "vue";
import Content from "@/components/layout/Content.vue";
import projects from "@/assets/projects.json";
import ScrollBlock from "@/components/shared/ScrollBlock.vue";
import Pagination from "@/components/shared/Pagination.vue";
import NavigationButton from "@/components/shared/NavigationButton.vue";

const projectsEl = ref<{ contentEl: HTMLDivElement | null } | null>(null);
const targetCard = ref(0);

const scroll = (back: boolean) => {
  const el = projectsEl.value?.contentEl;
  if (!el) return;

  const card = el.querySelector(".content");
  if (!card) return;

  const cardWidth = card.clientWidth + 20;
  el.scrollBy({
    left: back ? cardWidth : -cardWidth,
    behavior: "smooth",
  });
};

const onScroll = () => {
  const el = projectsEl.value?.contentEl;
  if (!el) return;

  const cards = el.querySelectorAll<HTMLDivElement>(
    ".content.scroll-snap-center"
  );

  let closestIndex = 0;
  let closestDistance = Infinity;

  cards.forEach((card, index) => {
    const cardRect = card.getBoundingClientRect();
    const centerX = window.innerWidth / 2;
    const cardCenterX = cardRect.left + cardRect.width / 2;
    const distance = Math.abs(centerX - cardCenterX);

    if (distance < closestDistance) {
      closestDistance = distance;
      closestIndex = index;
    }
  });

  targetCard.value = closestIndex;
};
</script>

<template>
  <div class="relative">
    <!-- Left arrow -->
    <NavigationButton :left="true" @scroll="scroll" v-if="targetCard > 0" />

    <!-- Scroll container -->
    <Content
      ref="projectsEl"
      id-ref="projects"
      :override="{ type: true, default: false }"
      @scroll.passive="onScroll"
      class="flex overflow-x-auto overflow-y-hidden gap-5 scroll-snap-x scroll-smooth hide-scrollbar sm:overflow-x-hidden"
    >
      <!-- Buffer -->
      <ScrollBlock />
      <ScrollBlock />

      <!-- Content -->
      <ScrollBlock
        v-for="(project, index) in projects"
        :key="index"
        :project="project"
        :selected="index === targetCard"
      />

      <!-- Buffer -->
      <ScrollBlock />
      <ScrollBlock />
    </Content>

    <Pagination :index="targetCard" :length="projects.length" />

    <!-- Right arrow -->
    <NavigationButton
      :left="false"
      @scroll="scroll"
      v-if="targetCard < projects.length - 1"
    />
  </div>
</template>
