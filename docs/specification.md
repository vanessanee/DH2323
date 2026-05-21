# Project Specification

## Comparison between geometric and physics based deformation models

**Course:** DH2323 Computer Graphics and Interaction, KTH Royal Institute of Technology  
**Authors:** Vanessa Neef (neef@kth.se), Elín Friðrika Hermannsdóttir (efhe@kth.se)  
**Date:** May 2026

---

## Research Question

How do geometric deformation methods and physics-based methods differ in terms of
visual realism, physical plausibility, and user perception when applied to interactive
object manipulation in VR?

## Background

Extended reality (XR) applications rely heavily on convincing graphical representation
and interaction of virtual objects. Real-time physics simulations are considered essential
for immersive XR environments because they enhance realism, user experience, and the
overall sense of presence within a virtual scene.

Different deformation techniques play an important role across a variety of XR
applications, including virtual clothing fitting, medical training, educational
environments, and interactive art installations. Selecting an appropriate deformation
model can be challenging since different approaches offer varying trade-offs between
realism, computational efficiency, controllability, and responsiveness.

## The Two Methods

### Free-Form Deformation (FFD)

A geometric technique that deforms a 3D mesh by embedding it within a flexible lattice
of control points. Originally introduced by Sederberg and Parry (1986), FFD enables
smooth, continuous deformation of arbitrary 3D geometry using trivariate Bernstein
polynomial blending functions. It was selected for this study due to its mathematical
rigour and widespread use in both academic research and production 3D software.

### Position-Based Dynamics (PBD)

A physics-based simulation method introduced by Müller et al. (2007), widely used for
real-time simulation of deformable bodies in games and interactive applications. Unlike
traditional physics approaches, PBD skips force calculation entirely and instead directly
adjusts vertex positions to satisfy physical constraints. It was selected as it underlies
Unity's built-in soft-body and cloth systems, making it directly relevant to real-time
VR development.

## Objectives

- Implement FFD using a 3×3×3 control point lattice in Unity 6
- Implement PBD using Unity's built-in physics system
- Design a within-subject VR user study comparing both methods
- Evaluate visual realism, physical plausibility, and user perception

## Evaluation

The study uses a within-subject design where participants experience both deformation
methods in an immersive VR environment using the Meta Quest 3. To minimise order bias,
half of the participants start with FFD and the other half start with PBD.

## Tools

- Unity 6
- C#
- Meta Quest 3
- GitHub

## References

- Sederberg & Parry (1986). Free-Form Deformation of Solid Geometric Models.
- Müller et al. (2007). Position Based Dynamics. Journal of Visual Communication
  and Image Representation.
- Sung et al. (2025). Real-Time Physics Simulation Method for XR Application.
