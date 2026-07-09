<script setup lang="ts">
import { inject, computed } from "vue";
export type Block = {
  description: string;
  link: string;
  section: string;
  sublinks?: Line[];
};

type Line = {
  description: string;
  link: string;
  section: string;
  index?: number | null;
};

const props = defineProps<{
  block: Block[];
}>();
const app: any = inject("app");

const lines = computed(() => {
  let lineCollection: Line[] = [];
  props.block.forEach((entry, key) => {
    lineCollection.push({
      description: entry.description,
      link: entry.link,
      section: entry.section,
      index: key,
    });

    if (entry.sublinks) {
      entry.sublinks.forEach((sublink) => {
        lineCollection.push({
          description: sublink.description,
          link: sublink.link,
          section: sublink.section,
          index: null,
        });
      });
      lineCollection.push({
        description: "}",
        link: "",
        section: "",
        index: null
      });
      lineCollection.push({
        description: "",
        link: "",
        section: "",
        index: null
      });
    }
  });
  return lineCollection;
});
</script>

<template>
  <div class="rounded bg-neutral-800 min-w-[300px] px-2 py-2 my-2">
    <div v-for="(line, index) in lines" class="code-block">
      <p class="flex pb-0">
        <span class="text-slate-600 px-2 mr-2 min-w-[25px] text-right">{{ index + 1 }}</span>
        <div v-if="line.link !== ''">
          <a
            :class="{ tab: line.index == null }"
            class="text-yellow-200 relative transition-colors cursor-pointer link"
            @click="app?.updateSection(line.section)"
            >{{ line.link }}
          </a>
          <span class="text-yellow-500">{{
            line.index !== null && block[line.index!].sublinks ? "() {" : "();"
          }}</span>
          <span class="text-gray-500 comment sm:opacity-0" v-if="line.description">
            // {{ line.description }}
          </span>
        </div>
        <div v-else>
          <span class="text-yellow-500">{{ line.description }}</span>
        </div>
      </p>
    </div>
  </div>
</template>

<style scoped>
.link:after {
  @apply content-[''] text-white absolute bottom-0 left-0 right-0 h-0.5 bg-current scale-x-0 origin-left;
}
.link:hover::after {
  @apply scale-x-100;
}
.code-block:hover {
  @apply active;
}
.comment {
  @apply transition-opacity duration-150;
}
.active .comment {
  @apply opacity-100;
}
.tab {
  @apply ml-3;
}
</style>
