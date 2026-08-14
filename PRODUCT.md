# Product

## Register

product

## Users

SpiritVale players who want to configure installed mods while playing, and mod authors who need a stable, low-friction settings API.

## Product Purpose

Provide one native-feeling Mod Configuration Menu inside SpiritVale's ESC settings popup. Players can discover and change mod options without editing configuration files. Mod authors can register typed settings through a small public API and receive persisted values and change callbacks.

## Brand Personality

Native, restrained, dependable. The interface should feel like part of SpiritVale rather than a separate developer overlay.

## Anti-references

Avoid detached debug windows, keyboard-only overlays, dense raw configuration editors, nested modal dialogs, decorative animation, and controls whose appearance conflicts with the game's existing settings UI.

## Design Principles

1. Meet players where settings already live.
2. Make common choices understandable without documentation.
3. Give mod authors typed primitives instead of UI plumbing.
4. Persist changes immediately and make reset behavior explicit.
5. Fail locally so one broken registration cannot make every mod's settings unusable.

## Accessibility & Inclusion

Use readable labels and descriptions, do not rely on color alone for state, provide visible values and focus states, keep text legible at common resolutions, and support keyboard capture cancellation with Escape.
