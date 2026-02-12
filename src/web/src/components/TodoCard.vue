<script setup lang="ts">
import type { TodoItem } from '@/api/todosService'

const props = defineProps<{
  todo: TodoItem
}>()

const emit = defineEmits<{
  click: [todo: TodoItem]
  vote: [todo: TodoItem]
}>()

function onCardClick() {
  emit('click', props.todo)
}
function onVoteClick(e: Event) {
  e.stopPropagation()
  emit('vote', props.todo)
}
</script>

<template>
  <div class="sticky-card" role="button" tabindex="0" @click="onCardClick">
    <div class="sticky-card-header">
      <span class="sticky-card-title" :class="{ done: todo.isDone }">{{ todo.title }}</span>
      <span v-if="todo.isDone" class="sticky-card-done">✓</span>
    </div>
    <p v-if="todo.notes" class="sticky-card-notes">{{ todo.notes }}</p>
    <div class="sticky-card-meta">
      <span v-if="todo.dueDate" class="sticky-card-due">Due: {{ new Date(todo.dueDate).toLocaleDateString() }}</span>
      <span v-if="todo.isDone && todo.completedAt" class="sticky-card-date">
        Done {{ new Date(todo.completedAt).toLocaleDateString() }}
      </span>
    </div>
    <div class="sticky-card-vote" @click="onVoteClick" role="button" tabindex="0" aria-label="Thumbs up">
      <span class="thumbs-icon" :class="{ voted: todo.currentUserVoted }" aria-hidden="true">👍</span>
      <span v-if="todo.voteCount > 0" class="vote-count">{{ todo.voteCount }}</span>
    </div>
  </div>
</template>

<style scoped>
.sticky-card {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: stretch;
  text-align: left;
  padding: 1rem;
  padding-bottom: 2rem;
  min-height: 7rem;
  border: var(--border-width) solid var(--line);
  border-radius: var(--radius);
  box-shadow:
    2px 2px 0 var(--line),
    3px 3px 0 rgba(0, 0, 0, 0.06);
  background: #fefce8;
  cursor: pointer;
  transition: transform 0.15s, box-shadow 0.15s;
  font-family: inherit;
  font-size: 0.9rem;
  color: var(--ink);
}
.sticky-card:hover {
  transform: translate(-1px, -1px);
  box-shadow:
    3px 3px 0 var(--line),
    5px 5px 0 rgba(0, 0, 0, 0.08);
}
.sticky-card-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 0.5rem;
  margin-bottom: 0.5rem;
}
.sticky-card-title {
  font-weight: 600;
  flex: 1;
  word-break: break-word;
}
.sticky-card-title.done {
  text-decoration: line-through;
  color: var(--pencil);
}
.sticky-card-done {
  flex-shrink: 0;
  color: var(--ink);
  font-size: 1rem;
}
.sticky-card-notes {
  margin: 0 0 0.5rem;
  font-size: 0.85rem;
  color: var(--pencil);
  line-height: 1.4;
  display: -webkit-box;
  -webkit-line-clamp: 3;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
.sticky-card-meta {
  margin-top: auto;
  font-size: 0.75rem;
  color: var(--pencil);
}
.sticky-card-due,
.sticky-card-date {
  opacity: 0.9;
}
.sticky-card-vote {
  position: absolute;
  bottom: 0.5rem;
  right: 0.5rem;
  display: flex;
  align-items: center;
  gap: 0.25rem;
  cursor: pointer;
  padding: 0.2rem;
  border-radius: 4px;
}
.sticky-card-vote:hover {
  background: rgba(0, 0, 0, 0.06);
}
.thumbs-icon {
  font-size: 1rem;
  opacity: 0.6;
}
.thumbs-icon.voted {
  opacity: 1;
}
.vote-count {
  font-size: 0.75rem;
  color: var(--pencil);
}
</style>
