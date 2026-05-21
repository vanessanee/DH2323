# Week 2 — Unity Development & Evaluation Design

## Getting into Unity

This week we moved from research into active development. We found two existing
Unity projects implementing similar deformation techniques, which gave us a
solid foundation to build on rather than starting entirely from scratch. We
began adapting these for our own comparison study.

In parallel, we started writing the mathematical sections of the report —
covering the theoretical foundations of both FFD and PBD, including the
Bernstein polynomial formulation and the PBD constraint projection equations.

## User evaluation design

We finalised the structure of our user study this week. The evaluation uses a
**within-subject design**, meaning every participant experiences both deformation
methods. To reduce order bias, half the participants will start with FFD and the
other half with PBD.

The study will be conducted in an immersive VR environment using the
**Meta Quest 3** at KTH. Participants will interact with deformable objects using
each method and then answer questions about their experience.

We spent time this week deciding what to actually measure and ask. Our evaluation
focuses on three things:

- **Visual realism** — does the deformation look convincing?
- **Physical plausibility** — does it behave the way you would expect?
- **User perception** — which method feels better to interact with?

<!-- ![User evaluation setup](images/evaluation_setup.png) -->

## Challenges

Deciding on the right questions for the user study was harder than expected.
We wanted to measure perception without leading participants toward a preferred
answer, which meant being careful about how each question was worded. We are
still refining the questionnaire.

## Next week

We plan to finish setting up the VR experiment on the Meta Quest 3 and
begin user testing at KTH. Development on both Unity implementations will
continue alongside the testing.
