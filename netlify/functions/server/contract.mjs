import { z } from 'zod';

export const levels = ['merchant', 'grow', 'climb', 'castle', 'escape'];
export const archetypes = ['ladder', 'crate', 'rope', 'beanstalk', 'beans', 'goose', 'generic', 'platform', 'stone'];
export const challenges = ['echo', 'towel', 'piano'];
export const roster = {
  merchant: { stall: 'stall', merchant: 'npc', cow: 'cow', beans: 'beans' },
  grow: { beanstalk: 'beanstalk', cloud: 'platform' },
  climb: { beanstalk: 'beanstalk', cloud: 'platform' },
  castle: { giant: 'giant', door: 'door', castle: 'castle' },
  escape: { giant: 'giant', goose: 'goose', egg: 'egg', exit: 'door' },
};
const id = z.string().regex(/^[a-z][a-z0-9_]{0,31}$/);
const text = n => z.string().max(n).refine(s => !/[\u0000-\u0008\u000b\u000c\u000e-\u001f]/.test(s), 'Control characters are not allowed');
export const worldSchema = z.object({
  id, archetype: z.enum([...archetypes, 'npc', 'cow', 'giant', 'door', 'stall', 'castle', 'egg']),
  state: text(48), x: z.number().min(-7).max(7), y: z.number().min(-2).max(3),
  size: z.number().min(.5).max(2.5), stability: z.number().min(0).max(1),
  climbable: z.boolean(), locked: z.boolean(), visible: z.boolean(),
}).strict();
export const requestSchema = z.object({
  sessionId: z.string().regex(/^demo-[a-zA-Z0-9-]{1,64}$/),
  levelId: z.enum(levels), playerText: text(400).refine(s => s.trim().length > 0),
  activeChallengeId: z.enum(['', ...challenges]), challengeSolved: z.boolean(),
  turnNumber: z.number().int().min(1).max(200), consecutiveSetbacks: z.number().int().min(0).max(200),
  inventory: z.array(z.enum(['cow', 'beans', 'goose', 'rope', 'key'])).max(5),
  worldEntities: z.array(worldSchema).min(1).max(24),
  recentHistory: z.array(z.object({ role: z.enum(['player', 'narrator']), text: text(350) }).strict()).max(6),
}).strict().superRefine((r, ctx) => {
  const entities = new Map(r.worldEntities.map(e => [e.id, e]));
  if (entities.size !== r.worldEntities.length || new Set(r.inventory).size !== r.inventory.length)
    ctx.addIssue({ code: 'custom', message: 'Duplicate entity or inventory item' });
  for (const [name, kind] of Object.entries(roster[r.levelId])) {
    if (entities.get(name)?.archetype !== kind) ctx.addIssue({ code: 'custom', message: `Missing or invalid required actor: ${name}` });
  }
  for (const e of entities.values()) if (!Object.hasOwn(roster[r.levelId], e.id) && !archetypes.includes(e.archetype))
    ctx.addIssue({ code: 'custom', message: 'Invented story actor' });
});
export const actionSchema = z.object({
  type: z.enum(['spawn', 'modify', 'transform', 'move', 'connect', 'destroy', 'grant_item', 'challenge', 'resolve_challenge', 'advance']),
  targetId: z.union([z.literal(''), id]), archetype: z.enum(['', ...archetypes]),
  property: z.enum(['', 'size', 'stability', 'climbable', 'locked', 'visible', 'label', 'x', 'y']),
  value: z.string().max(48).regex(/^[^<>\x00-\x1f]*$/), numberValue: z.number().min(-7).max(7),
  placement: z.enum(['', 'near_player', 'center', 'right']),
}).strict();
export const responseSchema = z.object({
  narration: z.string().min(1).max(350), mood: z.enum(['neutral', 'smug', 'amused', 'grudging']),
  twist: z.enum(['none', 'literal_misinterpretation', 'ironic_side_effect', 'riddle', 'helpful']),
  actions: z.array(actionSchema).max(3), challengeId: z.enum(['', ...challenges]), explanation: z.string().max(120),
}).strict();
export const responseJsonSchema = z.toJSONSchema(responseSchema);
delete responseJsonSchema.$schema;
export const fallback = (explanation = 'api_unavailable', narration = 'The narrator is negotiating with the internet. The offline understudy will take this turn.') => ({
  narration, mood: 'neutral', twist: 'none', actions: [], challengeId: '', explanation,
});

export function goalMet(level, entities, inventory) {
  const find = name => entities.get(name);
  const bridge = [...entities.values()].some(e => e.visible && e.climbable && e.stability >= .7 && e.size >= 1.5 && ['ladder', 'rope', 'platform', 'generic'].includes(e.archetype));
  switch (level) {
    case 'merchant': return inventory.has('beans');
    case 'grow': { const p = find('beanstalk'); return !!(p?.visible && p.climbable && p.size >= 1.5 && p.stability >= .7); }
    case 'climb': return bridge;
    case 'castle': return !!(find('door')?.visible && !find('door').locked) || bridge;
    case 'escape': return inventory.has('goose') && !!find('exit')?.visible && !find('exit').locked;
    default: return false;
  }
}

// Simulate every proposed action on a copy before displaying the narration.
// Unity independently revalidates against its actual authoritative scene state.
export function validateResponse(raw, request) {
  const parsed = responseSchema.safeParse(raw);
  if (!parsed.success) return fallback('validation_rejected', 'The narrator submitted an illegible stage direction. Try asking again.');
  const response = parsed.data;
  const entities = new Map(request.worldEntities.map(e => [e.id, { ...e }]));
  const inventory = new Set(request.inventory);
  let activeChallenge = request.activeChallengeId;
  const essential = name => Object.hasOwn(roster[request.levelId], name);
  function apply(a) {
    let e = entities.get(a.targetId);
    switch (a.type) {
      case 'spawn':
        if (!a.targetId || e || entities.size >= 24 || !archetypes.includes(a.archetype) || !['near_player', 'center', 'right'].includes(a.placement) || (a.numberValue !== 0 && (a.numberValue < .5 || a.numberValue > 2.5))) return false;
        entities.set(a.targetId, { id: a.targetId, archetype: a.archetype, size: a.numberValue || 1, stability: 1, climbable: ['ladder', 'rope', 'platform'].includes(a.archetype), visible: true, locked: false }); return true;
      case 'modify':
        if (!e) return false;
        if (['size', 'stability'].includes(a.property)) {
          if (a.numberValue < (a.property === 'size' ? .5 : 0) || a.numberValue > (a.property === 'size' ? 2.5 : 1)) return false;
          e[a.property] = a.numberValue; return true;
        }
        if (['climbable', 'locked', 'visible'].includes(a.property)) {
          if (!['true', 'false'].includes(a.value) || (a.property === 'locked' && e.archetype !== 'door') || (a.property === 'visible' && essential(e.id))) return false;
          e[a.property] = a.value === 'true'; return true;
        }
        return a.property === 'label';
      case 'transform':
        if (!e || essential(e.id) || !archetypes.includes(a.archetype)) return false;
        entities.set(e.id, { ...e, archetype: a.archetype, size: 1, stability: 1, visible: true, locked: false, climbable: ['ladder', 'rope', 'platform'].includes(a.archetype) }); return true;
      case 'move':
        if (!e || !['x', 'y'].includes(a.property) || a.numberValue < (a.property === 'x' ? -6 : -1) || a.numberValue > (a.property === 'x' ? 6 : 2.5)) return false;
        e[a.property] = a.numberValue; return true;
      case 'connect':
        if (!e || !entities.has(a.value) || e.id === a.value) return false;
        e.stability = 1; return true;
      case 'destroy':
        if (!e || essential(e.id)) return false;
        entities.delete(e.id); return true;
      case 'grant_item':
        if (!['beans', 'goose', 'rope', 'key'].includes(a.value) || (a.value === 'beans' && request.levelId !== 'merchant') || (a.value === 'goose' && request.levelId !== 'escape')) return false;
        inventory.add(a.value);
        if (a.value === 'beans') { inventory.delete('cow'); for (const name of ['cow', 'beans']) if (entities.has(name)) entities.get(name).visible = false; }
        if (a.value === 'goose' && entities.has('goose')) entities.get('goose').visible = false;
        return true;
      case 'challenge':
        if (!challenges.includes(a.value) || activeChallenge || request.challengeSolved) return false;
        activeChallenge = a.value; return true;
      case 'resolve_challenge': return request.challengeSolved;
      case 'advance': return goalMet(request.levelId, entities, inventory);
      default: return false;
    }
  }
  if (response.actions.some(a => !apply(a)) || (response.challengeId && (request.challengeSolved || (activeChallenge && activeChallenge !== response.challengeId))))
    return fallback('validation_rejected', 'The stage crew vetoed that impossible instruction. Nothing changed; try another approach, Jack.');
  return response;
}
