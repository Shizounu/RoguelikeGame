# Content definitions

Definition JSON files are loaded recursively from this folder. They do not
include a `Version` field; the supported fields are described by the typed
definitions in the content catalog.

NPC definitions live in `Npcs` and reference a dialogue graph by its stable
`DialogueTreeId`, for example `Dialogues.Haven.MaraTheBaker`. The referenced
tree lives in `Dialogues` and defines explicit entry-node references plus
nodes with stable IDs and prioritized branches. This graph shape is intended
to support a future dialogue editor inspired by
[Shizounu/DialogueSystem](https://github.com/Shizounu/DialogueSystem).

The current runtime uses the highest-priority entry sentence when an NPC is
interacted with. Branch traversal and editor tooling are not implemented yet.
