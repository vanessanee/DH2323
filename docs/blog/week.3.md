# Week 3 — User Testing & Wrapping Up

## User evaluation

This week we ran our user study with **4 participants** recruited from KTH,
all of whom were students. The sessions were held in Middle, where we set up
the Meta Quest 3 and guided each participant through both deformation conditions.

Each participant interacted with deformable objects using both FFD and PBD,
and filled out a questionnaire afterwards rating each method on visual realism,
physical plausibility, and overall experience.

<!-- ![User testing session](images/user_testing.png) -->

## Results

Text about what participants thought — which method they preferred, any
patterns in the responses, anything surprising.

<!-- ![Questionnaire results](images/results_chart.png) -->

## Blog & report

Alongside the user testing we finalised the report and set up this blog to
document the project progress. The report covers the full mathematical
background of both methods, the Unity implementation details, and the
evaluation findings.

## Challenges

Getting PBD to behave correctly in Unity took more effort than expected.
The soft-body simulation kept producing unstable results until we adjusted
the constraint solver iterations and stiffness parameters to values that
produced physically plausible behaviour in real time.

## Wrapping up

With the user study complete and both implementations working, the project
is now finished. Head over to the [Results](../results.md) and
[Final Report](../report/final_report.md) pages for the full findings.
