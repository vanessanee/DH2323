# Week 1 — Project Setup & Research

## Getting organised

We started by setting up our GitHub repository and creating a structured issue
tracker to keep the project organised. We created issues for the following categories:

- **User Evaluation** — planning the VR study and participant recruitment
- **Papers & Resources** — collecting and tracking relevant literature
- **Progress** — weekly updates and milestones
- **Output** — the Unity program and source code
- **Report** — writing and structuring the final paper

This gave us a clear overview of what needed to be done and helped us divide
the work between us from the start.

## Choosing our methods

After an initial literature review we decided on the two deformation methods
we would implement and compare:

**Free-Form Deformation (FFD)** is a geometric technique that deforms a mesh by
embedding it in a lattice of control points. Moving a control point smoothly
displaces the surrounding geometry using Bernstein polynomial weights. It requires
no physics simulation, making it fast and predictable.

**Position-Based Dynamics (PBD)** is a physics-based method that simulates soft-body
behaviour by iteratively satisfying constraints between connected vertices. It is
the method underlying Unity's built-in cloth and soft-body systems.

We spent most of this week reading the original papers — Sederberg & Parry (1986)
for FFD and Müller et al. (2007) for PBD — and searching for existing Unity
implementations to use as reference.

## Challenges

Getting started with the FFD math was trickier than expected. Understanding how
mesh vertices get mapped into the local STU coordinate space before the Bernstein
polynomials can be applied took some time to wrap our heads around.

## Next week

We plan to find and study existing Unity source code for both FFD and PBD to
give us a clearer starting point before writing our own implementations.
