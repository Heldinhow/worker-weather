# Aura Home — Website Design

**Date**: 2026-04-05  
**Status**: Approved  
**Concept**: "The best technology is technology you don't feel, only comfort."

---

## 1. Concept & Vision

Aura Home is not marketed — it is *experienced*. The website embodies the philosophy that "the best technology is technology you don't feel, only comfort." Every interaction feels like a gentle breath, never abrupt. Visitors leave feeling they've glimpsed a more tranquil way of living, not another smart home pitch. Ethereal, tranquil, with a touch of Zen.

---

## 2. Visual Strategy

### Core Image
Morning light filtering through linen curtains, dust particles floating in amber beams. A single matte-white smart panel, almost camouflaged against the wall.

### Light & Shadow Language
| Moment | Palette | Feel |
|--------|---------|------|
| Dawn (hero start) | Cool pale gray, subtle blue undertones | Still, quiet |
| Day (hover states) | Warm mica white, soft golden scatter | Alive, responsive |
| Dusk (scene transitions) | Deep warm orange (halo) → warm gray | Transitioning, soft |
| Night (evening scenes) | Deep space gray, soft amber pools | Settled, restful |

### Material Palette
| Role | Hex | Notes |
|------|-----|-------|
| Background | `#F5F3F0` | Warm gray, almost linen |
| Primary | `#E8E4DF` | Mica white |
| Secondary | `#D4CFC7` | Light oak undertone |
| Accent Halo | `#F5E6D3` | Very pale warm orange |
| Text Primary | `#3D3D3D` | Deep space gray |
| Text Secondary | `#8A8580` | Muted warm gray |

*Forbidden: Tech blue, saturated colors, metallic reflections.*

### Typography
- **Headings**: Helvetica Now Thin / Inter Tight — 300 weight, letter-spacing: 0.15em
- **Body**: Source Serif 4 — 400 weight, line-height: 1.8

---

## 3. Page Structure

### 3.1 Hero Section (100vh)
- **Abstract light art canvas** — Particles drift slowly in simulated sunbeam with gentle physics
- **Away Mode** (default): Particles cool, sparse, barely visible. Light beam faint.
- **Home Mode** (on scroll/hover): Particles warm and abundant, light beam intensifies with amber glow
- **Content**: "AURA HOME" in thin, spaced capitals — no tagline
- **Scroll indicator**: Thin vertical line, gentle pulse, disappears after first scroll

### 3.2 Invisible Control Section (~80vh)
- Large central matte-white panel illustration (CSS-drawn)
- Mouse movement simulates human presence — particles respond, ambient light shifts
- **Temperature display**: Fluctuates 21.0°–23.5° based on cursor proximity
- **Light level indicator**: "Reading" (bright) ↔ "Relaxing" (dim) based on movement
- No toggles that "click on" — everything shifts fluidly

### 3.3 Scene Narrative Section
- **Split-screen layout**: Left = scene (abstract light art), Right = poetic text
- **Scenes** (vertical flow):
  1. *Morning* → "Light that knows when to wake you"
  2. *Reading* → "Pages turn, shadows adjust"
  3. *Bathing* → "Steam rises, warmth holds"
  4. *Evening* → "The day settles, so do you"
- Connected by **flowing light strip** — vertical gradient line, soft pulse
- Scroll-triggered crossfades, never hard cuts

### 3.4 Folded Specs Section
- Cards styled as cream paper with subtle texture
- **Closed state**: Only experiential tagline visible ("0.1-second ultra-fast response")
- **Click to reveal**: Manual trigger, 600ms accordion expand
- **Inside**: Technical context as prose, not specsheets
- Click again to fold back

### 3.5 Footer
- Minimal: logo, "Technology you don't feel", invisible nav links
- Ethereal gradient fade into background

---

## 4. Component Inventory

| Component | States | Behavior |
|-----------|--------|----------|
| Particle Canvas | away, home, idle | Responds to scroll + cursor with physics-based drift |
| Scene Card | default, active | Crossfades content, light strip pulses |
| Spec Accordion | collapsed, expanded, collapsing | Click to toggle, 600ms ease-out |
| Navigation | hidden, visible | Appears on scroll-up, fades |
| Scroll Indicator | idle, animating | Vertical pulse, hides post-scroll |

---

## 5. Technical Approach

| Aspect | Choice |
|--------|--------|
| Stack | Single `index.html` + `styles.css` + `script.js` |
| Particle System | Canvas API + requestAnimationFrame, 80–120 particles |
| Scroll Detection | Intersection Observer + scroll event |
| Animations | CSS transitions + keyframes, zero dependencies |
| Fonts | Google Fonts (Inter Tight 300, Source Serif 4) |
| Performance Target | < 200KB total, < 2s load on 4G |

---

## 6. Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Technical Stack | Vanilla HTML/CSS/JS | Lightest footprint, full control |
| Hero Visual | Abstract light art (Canvas) | Most ethereal, aligns with "invisible" philosophy |
| Spec Expand | Manual click/hold | Intentional discovery, gentle reveal |

---

*Next: See `docs/plans/YYYY-MM-DD-aura-home-implementation.md` for implementation plan.*
