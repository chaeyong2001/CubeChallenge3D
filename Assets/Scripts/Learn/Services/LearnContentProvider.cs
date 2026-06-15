using System;
using System.Collections.Generic;
using System.Linq;
using CubeChallenge3D.Learn.Model;

namespace CubeChallenge3D.Learn.Services
{
    public sealed class LearnContentProvider
    {
        // Whole-cube orientation equivalent to x2: white is on the bottom.
        private const string WhiteBottomSolved =
            "DDDDDDDDDRRRRRRRRRBBBBBBBBBUUUUUUUUULLLLLLLLLFFFFFFFFF";

        private readonly List<LearnCategoryData> categories;

        public LearnContentProvider()
        {
            categories = BuildContent();
            LearnDemoValidator.ValidateBeginnerDemos(categories);
        }

        public IReadOnlyList<LearnCategoryData> GetCategories()
        {
            return categories.OrderBy(category => category.order).ToList().AsReadOnly();
        }

        public IReadOnlyList<LearnLessonData> GetLessons(string categoryId)
        {
            LearnCategoryData category = categories.FirstOrDefault(item =>
                string.Equals(item.categoryId, categoryId, StringComparison.OrdinalIgnoreCase));
            return category == null
                ? new List<LearnLessonData>().AsReadOnly()
                : category.lessons.OrderBy(lesson => lesson.order).ToList().AsReadOnly();
        }

        public LearnLessonData GetLesson(string lessonId)
        {
            return categories.SelectMany(category => category.lessons).FirstOrDefault(lesson =>
                string.Equals(lesson.lessonId, lessonId, StringComparison.OrdinalIgnoreCase));
        }

        private static List<LearnCategoryData> BuildContent()
        {
            var result = new List<LearnCategoryData>
            {
                Category("basics", "Learn Basics", "Orientation, cube faces, and turn direction.", 0,
                    Explanation(
                        "orientation",
                        "Cube Orientation",
                        "Set a stable Front, Top, and Right before following moves.",
                        "Front is the face you are looking at. Top is the face on top. Right is the face on your right.\n\nKeep this orientation while following a move sequence.",
                        0,
                        "Do not rotate the whole cube unless instructed.",
                        "Moves use the current Front, Top, and Right orientation."),
                    Explanation(
                        "faces",
                        "Cube Faces",
                        "Learn the six standard face letters.",
                        "U = Top\nD = Bottom\nR = Right\nL = Left\nF = Front\nB = Back",
                        1,
                        "Face letters describe physical faces, not screen movement.",
                        "Center stickers identify each face."),
                    Demo(
                        "turn_direction",
                        "Clockwise and Counter-clockwise",
                        "Read normal, prime, and double turns.",
                        "A move without ' is clockwise. A move with ' is counter-clockwise. A move with 2 turns 180 degrees.\n\nClockwise always means clockwise when looking directly at that face.",
                        2,
                        "NotationMove",
                        new[] { "R", "R'", "R2" },
                        null,
                        null,
                        "Compare a normal, prime, and double right-face turn.",
                        "Learn how normal, prime, and double notation changes one face.",
                        null,
                        null,
                        "Look directly at the moving face.",
                        "R2 and R2' describe the same 180-degree result."))
            };

            var notation = new List<LearnLessonData>();
            AddFaceNotation(notation, "R", "Right", 0);
            AddFaceNotation(notation, "U", "Top", 3);
            AddFaceNotation(notation, "F", "Front", 6);
            AddFaceNotation(notation, "L", "Left", 9);
            AddFaceNotation(notation, "D", "Bottom", 12);
            AddFaceNotation(notation, "B", "Back", 15);
            result.Add(Category(
                "notation",
                "Notation",
                "Practice all 18 basic face moves.",
                1,
                notation.ToArray()));

            result.Add(Category("beginner", "Beginner Method", "A seven-step path for solving a 3x3 cube.", 2,
                WhiteCrossLesson(),
                WhiteCornersLesson(),
                SecondLayerLesson(),
                YellowCrossLesson(),
                YellowFaceLesson(),
                LastLayerCornersLesson(),
                LastLayerEdgesLesson()));

            result.Add(Category("formulas", "Formula Practice", "Practice move patterns and use them in the correct case.", 3,
                RightTriggerLesson(),
                LeftTriggerLesson(),
                SledgehammerLesson(),
                YellowCrossFormulaLesson(),
                RightAlgorithmLesson()));

            return result;
        }

        private static LearnLessonData RightTriggerLesson()
        {
            return new LearnLessonData
            {
                lessonId = "formula_right_trigger",
                title = "Right Trigger",
                shortDescription = "Open the right slot, move a top corner into place, then close the slot.",
                bodyText =
                    "The beginner Right Trigger is R U R'.\n\n"
                    + "Use it when the target corner is in the top layer above its matching slot and must enter from the right side.\n\n"
                    + "1. R opens the right-side slot.\n"
                    + "2. U moves the target corner over the open slot.\n"
                    + "3. R' closes the slot and inserts the corner.\n\n"
                    + "R U R' U' is also commonly practised as a repeating four-move trigger, but the final U' is not required for this basic corner insertion case.",
                order = 0,
                has3DDemo = true,
                demoType = "FormulaRightTrigger",
                demoPurpose = "Show a real first-layer corner insertion instead of applying moves to a solved cube.",
                demoGoalDescription = "Insert the highlighted white corner into its matching first-layer slot.",
                moveNotations = new[] { "R", "U", "R'" },
                demoMoves = Array.Empty<string>(),
                demoSubsteps = new[]
                {
                    new LearnStepDemoData
                    {
                        substepTitle = "Right-side Corner Insertion",
                        instructionText =
                            "The highlighted corner is above its matching slot. Keep white on the bottom and use R U R' to insert it from the right.",
                        goalSetupNotation = "L' B L U F U' B' F'",
                        startFaceletString = WhiteBottomSolved,
                        demoMoves = new[] { "R", "U", "R'" },
                        moveInstructions = new[]
                        {
                            "R: Open the right-side slot.",
                            "U: Move the highlighted corner over the open slot.",
                            "R': Close the slot and insert the corner."
                        },
                        targetDescription = "Yellow marks the target white corner; cyan marks its matching first-layer slot.",
                        highlightedCubieHint = "Target white corner",
                        targetSlotHint = "Right-side first-layer corner slot",
                        expectedWhiteFaceName = "Left",
                        wholeCubeYawQuarterTurns = 2,
                        generateSetupFromDemoMoves = true,
                        targetStartX = -1,
                        targetStartY = 1,
                        targetStartZ = 1,
                        targetGoalX = -1,
                        targetGoalY = -1,
                        targetGoalZ = 1
                    }
                },
                keyPoints = new[]
                {
                    "Match all three corner colors with the surrounding centers before inserting.",
                    "Keep the solved white cross on the bottom.",
                    "R opens the slot and R' restores it.",
                    "Use the mirrored Left Trigger when the corner must enter from the left."
                }
            };
        }

        private static LearnLessonData LeftTriggerLesson()
        {
            return FormulaCaseLesson(
                "formula_left_trigger",
                "Left Trigger",
                "Insert a matching top corner through the left-side slot.",
                "The Left Trigger is L' U' L.\n\n"
                + "Use it when a matched corner is above its target and must enter from the left side.\n\n"
                + "1. L' opens the left-side slot.\n"
                + "2. U' moves the target corner over the open slot.\n"
                + "3. L closes the slot and inserts the corner.",
                1,
                "FormulaLeftTrigger",
                "Left-side Corner Insertion",
                "Keep white on the bottom. Insert the highlighted corner into the matching left-side first-layer slot.",
                "L U' R' F U' R F' L'",
                new[] { "L'", "U'", "L" },
                new[]
                {
                    "L': Open the left-side slot.",
                    "U': Move the highlighted corner over the open slot.",
                    "L: Close the slot and insert the corner."
                },
                "Target white corner",
                "Left-side first-layer corner slot",
                1, 1, 1,
                1, -1, 1,
                "Match all three corner colors before inserting.",
                "Keep the white cross on the bottom.",
                "The Left Trigger mirrors the Right Trigger.");
        }

        private static LearnLessonData SledgehammerLesson()
        {
            return FormulaCaseLesson(
                "formula_sledgehammer",
                "Sledgehammer",
                "Insert or reorient a prepared corner-edge pair without rotating the whole cube.",
                "The Sledgehammer is R' F R F'.\n\n"
                + "It is commonly used in F2L and last-layer cases because it moves a corner-edge pair while changing edge orientation.\n\n"
                + "In this demo, the highlighted corner and its matching edge are prepared together above their slot. The four moves place the pair into the first two layers.",
                2,
                "FormulaSledgehammer",
                "Prepared F2L Pair Insertion",
                "Watch the prepared corner-edge pair enter the highlighted front-side F2L slot.",
                "R U R' U R U2 R'",
                new[] { "R'", "F", "R", "F'" },
                new[]
                {
                    "R': Move the prepared pair away from the slot.",
                    "F: Open the front-side slot.",
                    "R: Bring the pair into alignment.",
                    "F': Close the slot with the pair inserted."
                },
                "Prepared corner-edge pair",
                "Front-side F2L slot",
                -1, 1, 1,
                -1, -1, 1,
                "Treat the corner and edge as one pair.",
                "The completed white cross remains on the bottom.",
                "The result restores the first two layers.");
        }

        private static LearnLessonData YellowCrossFormulaLesson()
        {
            return FormulaCaseLesson(
                "formula_yellow_cross",
                "Yellow Cross Formula",
                "Turn a yellow edge line into a yellow cross.",
                "Use F R U R' U' F' to orient the last-layer edges.\n\n"
                + "Ignore the yellow corners. In this example the top face begins with a yellow line. Hold the line in the demonstrated orientation and perform the formula once.\n\n"
                + "The result is a yellow cross while the first two layers remain solved.",
                3,
                "FormulaYellowCross",
                "Yellow Line to Cross",
                "Orient the four yellow top edges to form a cross.",
                "R U R' U R U2 R'",
                new[] { "F", "R", "U", "R'", "U'", "F'" },
                new[]
                {
                    "F: Open the front face for the edge-orientation sequence.",
                    "R: Raise the right layer.",
                    "U: Move the top edges through the open area.",
                    "R': Restore the right layer.",
                    "U': Restore the top alignment.",
                    "F': Close the front face and reveal the yellow cross."
                },
                "Yellow top edges",
                "Top yellow cross",
                0, 1, 1,
                0, 1, 1,
                "Only the four yellow edge stickers matter.",
                "Keep the first two layers solved.",
                "Use the correct line or L orientation before applying the formula.");
        }

        private static LearnLessonData RightAlgorithmLesson()
        {
            return FormulaCaseLesson(
                "formula_right_algorithm",
                "Right Algorithm",
                "Orient the final yellow corners with the Sune algorithm.",
                "The Right Algorithm, often called Sune, is R U R' U R U2 R'.\n\n"
                + "Use it after the yellow cross is complete. Hold the yellow-corner pattern in the demonstrated orientation, then perform all seven moves without rotating the cube.\n\n"
                + "This example finishes the full yellow face. In another case, reposition the top layer and repeat as instructed.",
                4,
                "FormulaRightAlgorithm",
                "Sune to Yellow Face",
                "Orient the remaining yellow corners so all nine top stickers become yellow.",
                "F2 U R' L F2 L' R U F2",
                new[] { "R", "U", "R'", "U", "R", "U2", "R'" },
                new[]
                {
                    "R: Begin the right-hand corner cycle.",
                    "U: Move the next yellow corner into the working area.",
                    "R': Restore the right side.",
                    "U: Continue the top-corner cycle.",
                    "R: Reopen the right side.",
                    "U2: Move the final corners through the working area.",
                    "R': Restore the cube and complete the yellow face."
                },
                "Unoriented yellow corners",
                "Complete yellow top face",
                1, 1, 1,
                1, 1, 1,
                "The yellow cross must already be complete.",
                "Keep the cube orientation fixed for all seven moves.",
                "This algorithm orients corners; their positions are handled separately.");
        }

        private static LearnLessonData FormulaCaseLesson(
            string lessonId,
            string title,
            string shortDescription,
            string bodyText,
            int order,
            string demoType,
            string caseTitle,
            string instruction,
            string goalSetupNotation,
            string[] demoMoves,
            string[] moveInstructions,
            string pieceHint,
            string slotHint,
            int targetStartX,
            int targetStartY,
            int targetStartZ,
            int targetGoalX,
            int targetGoalY,
            int targetGoalZ,
            params string[] keyPoints)
        {
            return new LearnLessonData
            {
                lessonId = lessonId,
                title = title,
                shortDescription = shortDescription,
                bodyText = bodyText,
                order = order,
                has3DDemo = true,
                demoType = demoType,
                demoPurpose = instruction,
                demoGoalDescription = instruction,
                moveNotations = demoMoves,
                demoMoves = Array.Empty<string>(),
                demoSubsteps = new[]
                {
                    new LearnStepDemoData
                    {
                        substepTitle = caseTitle,
                        instructionText = instruction,
                        goalSetupNotation = goalSetupNotation,
                        startFaceletString = WhiteBottomSolved,
                        demoMoves = demoMoves,
                        moveInstructions = moveInstructions,
                        targetDescription = instruction,
                        highlightedCubieHint = pieceHint,
                        targetSlotHint = slotHint,
                        wholeCubeYawQuarterTurns = 2,
                        generateSetupFromDemoMoves = true,
                        targetStartX = targetStartX,
                        targetStartY = targetStartY,
                        targetStartZ = targetStartZ,
                        targetGoalX = targetGoalX,
                        targetGoalY = targetGoalY,
                        targetGoalZ = targetGoalZ
                    }
                },
                keyPoints = keyPoints ?? Array.Empty<string>()
            };
        }

        private static LearnLessonData WhiteCrossLesson()
        {
            return new LearnLessonData
            {
                lessonId = "beginner_white_cross",
                title = "Step 1: White Cross",
                shortDescription = "Place all four white edges into the bottom cross.",
                bodyText =
                    "The goal is to build a white cross on the bottom.\n\n"
                    + "1. Find a white edge piece.\n"
                    + "2. If needed, move it to the top layer without losing pieces already solved.\n"
                    + "3. Turn the top layer until the edge's side color matches its center.\n"
                    + "4. Keep that matching center facing you.\n"
                    + "5. Turn the front face 180 degrees to send the white edge to the bottom.\n"
                    + "6. Repeat until four white edges form a cross.\n\n"
                    + "The four demos are representative cases, not one fixed formula. "
                    + "The preparation moves change depending on where and how the edge starts.",
                order = 0,
                has3DDemo = true,
                demoType = "BeginnerMultiStep",
                demoPurpose = "Build the complete bottom white cross one edge case at a time.",
                demoGoalDescription = "Complete all four white edge placements on the bottom.",
                moveNotations = Array.Empty<string>(),
                demoMoves = Array.Empty<string>(),
                demoSubsteps = new[]
                {
                    CrossSubstep(
                        "Case 1 / 4: White-Green Already Aligned",
                        2,
                        "green",
                        new[] { "F2" },
                        new[]
                        {
                            "The green side already matches the green center. Turn the front face 180 degrees to insert the edge."
                        },
                        0, 1, 1),
                    CrossSubstep(
                        "Case 2 / 4: White-Red Needs Alignment",
                        -1,
                        "red",
                        new[] { "U'", "F2" },
                        new[]
                        {
                            "Turn the top layer until the red side of the white-red edge matches the red center.",
                            "The edge is aligned. Turn the front face 180 degrees to insert it into the bottom cross."
                        },
                        1, 1, 0),
                    CrossSubstep(
                        "Case 3 / 4: White-Blue in the Middle Layer",
                        0,
                        "blue",
                        new[] { "F", "F2" },
                        new[]
                        {
                            "Move the white-blue edge out of the middle layer and onto the top-front position.",
                            "The blue side now matches the blue center. Turn the front face 180 degrees to insert it."
                        },
                        1, 0, 1),
                    CrossSubstep(
                        "Case 4 / 4: White-Orange Flipped on Top",
                        1,
                        "orange",
                        new[] { "F", "R", "U", "F2" },
                        new[]
                        {
                            "Move the flipped white-orange edge away from the front slot.",
                            "Use the side face to reorient the edge.",
                            "Turn the top layer until the orange side matches the orange center.",
                            "The edge is aligned. Turn the front face 180 degrees to complete the placement."
                        },
                        0, 1, 1)
                },
                keyPoints = new[]
                {
                    "White stickers alone are not enough; every side color must match its center.",
                    "Use top-layer turns to align an edge before inserting it.",
                    "The final insertion is F2 only after the matching center faces you.",
                    "Previously solved cross edges should remain in place."
                }
            };
        }

        private static LearnLessonData WhiteCornersLesson()
        {
            return new LearnLessonData
            {
                lessonId = "beginner_white_corners",
                title = "Step 2: White Corners",
                shortDescription = "Complete the white first layer without breaking the cross.",
                bodyText =
                    "Keep the completed white cross on the bottom.\n\n"
                    + "1. Find a corner containing white.\n"
                    + "2. Read its other two colors to identify its correct slot.\n"
                    + "3. Turn the top layer until the corner is above that slot.\n"
                    + "4. If white faces a side, insert from that side with a three-move trigger.\n"
                    + "5. If white faces up, first turn it to the side, realign it, then insert.\n"
                    + "6. If a white corner is twisted or in the wrong bottom slot, move it to the top and insert it again.\n\n"
                    + "Repeat for all four corners. The result is a complete white face and a matching first row on every side.",
                order = 1,
                has3DDemo = true,
                demoType = "BeginnerCornerStep",
                demoPurpose = "Learn the four common white-corner insertion situations.",
                demoGoalDescription = "Place one white corner correctly while keeping the bottom cross solved.",
                moveNotations = Array.Empty<string>(),
                demoMoves = Array.Empty<string>(),
                demoSubsteps = new[]
                {
                    CornerSubstep(
                        "Case 1 / 4: White Faces Right",
                        "The corner is above the front-right slot and white faces the right side. Insert it with the right-side trigger.",
                        "L U' R' F U' R F' L'",
                        new[] { "L'", "U'", "L" },
                        new[]
                        {
                            "Open the right-side slot without disturbing the white cross.",
                            "Move the corner over the open slot.",
                            "Close the slot to insert the corner into the first layer."
                        },
                        "Right",
                        1, 1, 1,
                        1, -1, 1),
                    CornerSubstep(
                        "Case 2 / 4: White Faces Left",
                        "The corner is above the front-left slot and white faces the left side. Use the mirrored trigger.",
                        "L' B L U F U' B' F'",
                        new[] { "R", "U", "R'" },
                        new[]
                        {
                            "Open the left-side slot while preserving the cross.",
                            "Move the corner over the open slot.",
                            "Close the slot to complete the mirrored insertion."
                        },
                        "Left",
                        -1, 1, 1,
                        -1, -1, 1),
                    CornerSubstep(
                        "Case 3 / 4: White Faces Up",
                        "White points upward, so a direct three-move insert will not solve the corner. Turn it to the side, realign it, then insert.",
                        "L U' R' F U' R F' L'",
                        new[] { "L'", "U2", "L", "U", "L'", "U'", "L" },
                        new[]
                        {
                            "Move the top-facing white corner away from the slot.",
                            "Rotate the top layer to create space for reorientation.",
                            "Return the side face so white now points sideways.",
                            "Realign the corner above its correct slot.",
                            "Open the right-side slot.",
                            "Move the corner over the slot.",
                            "Close the slot to finish the insertion."
                        },
                        "Up",
                        1, 1, 1,
                        1, -1, 1),
                    CornerSubstep(
                        "Case 4 / 4: Corner Twisted in Bottom",
                        "The corner is in the bottom layer but is twisted. Take it out to the top, realign it, and insert it correctly.",
                        "L U' R' F U' R F' L'",
                        new[] { "L'", "U'", "L", "U", "L'", "U'", "L" },
                        new[]
                        {
                            "Open the slot to remove the twisted corner from the bottom layer.",
                            "Move the corner into the top layer.",
                            "Close the slot; the white cross remains solved.",
                            "Turn the top layer to realign the corner.",
                            "Open the slot for the correct insertion.",
                            "Move the corner over the slot.",
                            "Close the slot with the corner correctly oriented."
                        },
                        "Right",
                        1, -1, 1,
                        1, -1, 1)
                },
                keyPoints = new[]
                {
                    "A corner belongs where all three of its colors match the surrounding centers.",
                    "Keep the white cross on the bottom throughout this step.",
                    "Right-facing and left-facing white stickers use mirrored three-move inserts.",
                    "Top-facing or incorrectly placed corners must be reoriented before insertion.",
                    "After all four corners, the entire first layer must match, not only the white face."
                }
            };
        }

        private static LearnStepDemoData CornerSubstep(
            string title,
            string instruction,
            string goalSetupNotation,
            string[] demoMoves,
            string[] moveInstructions,
            string expectedWhiteFaceName,
            int targetStartX,
            int targetStartY,
            int targetStartZ,
            int targetGoalX,
            int targetGoalY,
            int targetGoalZ)
        {
            return new LearnStepDemoData
            {
                substepTitle = title,
                instructionText = instruction,
                goalSetupNotation = goalSetupNotation,
                startFaceletString = WhiteBottomSolved,
                demoMoves = demoMoves,
                moveInstructions = moveInstructions,
                targetDescription = "Match all three corner colors, then insert the corner without breaking the white cross.",
                highlightedCubieHint = "Target white corner",
                targetSlotHint = targetGoalX > 0 ? "Bottom-front-right corner slot" : "Bottom-front-left corner slot",
                targetSideColorName = string.Empty,
                expectedWhiteFaceName = expectedWhiteFaceName,
                wholeCubeYawQuarterTurns = 2,
                generateSetupFromDemoMoves = true,
                targetStartX = targetStartX,
                targetStartY = targetStartY,
                targetStartZ = targetStartZ,
                targetGoalX = targetGoalX,
                targetGoalY = targetGoalY,
                targetGoalZ = targetGoalZ
            };
        }

        private static LearnLessonData SecondLayerLesson()
        {
            return BeginnerAlgorithmLesson(
                "beginner_second_layer",
                "Step 3: Second Layer",
                "Insert the four non-yellow edges into the middle layer.",
                "Keep the completed white layer on the bottom.\n\n"
                + "1. Find a top-layer edge with no yellow sticker.\n"
                + "2. Match its front color with the same-color center.\n"
                + "3. Look at the edge's top color to decide whether it belongs left or right.\n"
                + "4. Use the matching eight-move insertion.\n"
                + "5. If no usable edge is on top, eject an incorrect middle edge first.\n\n"
                + "Repeat until the first two layers are solved around all four sides.",
                2,
                "BeginnerSecondLayer",
                "Complete the middle ring while preserving the white first layer.",
                new[]
                {
                    AlgorithmCase(
                        "Case 1 / 3: Edge Goes Right",
                        "Match the front sticker to its center. The top sticker matches the right center, so insert the edge to the right.",
                        "R U R' U R U2 R'",
                        new[] { "U", "R", "U'", "R'", "U'", "F'", "U", "F" },
                        "Target middle-layer edge",
                        "Front-right middle slot",
                        0, 1, 1,
                        -1, 0, 1),
                    AlgorithmCase(
                        "Case 2 / 3: Edge Goes Left",
                        "Match the front sticker to its center. The top sticker matches the left center, so use the mirrored insertion.",
                        "R U R' U R U2 R'",
                        new[] { "U'", "L'", "U", "L", "U", "F", "U'", "F'" },
                        "Target middle-layer edge",
                        "Front-left middle slot",
                        0, 1, 1,
                        1, 0, 1),
                    AlgorithmCase(
                        "Case 3 / 3: Wrong Edge in Middle",
                        "When every usable edge is trapped in the middle layer, eject the incorrect edge, align it on top, then insert it correctly.",
                        "R U R' U R U2 R'",
                        new[]
                        {
                            "U", "R", "U'", "R'", "U'", "F'", "U", "F",
                            "U",
                            "U", "R", "U'", "R'", "U'", "F'", "U", "F"
                        },
                        "Incorrect middle-layer edge",
                        "Eject and refill the front-right middle slot",
                        -1, 0, 1,
                        -1, 0, 1)
                },
                "Use only top-layer edges without yellow.",
                "Match the front color before choosing left or right.",
                "A correct insertion restores the white layer automatically.",
                "The second layer is complete when the bottom two rows of every side match their centers.");
        }

        private static LearnLessonData YellowCrossLesson()
        {
            return BeginnerAlgorithmLesson(
                "beginner_yellow_cross",
                "Step 4: Yellow Cross",
                "Orient the four yellow edges on the top face.",
                "Keep the solved first two layers underneath.\n\n"
                + "Ignore the yellow corners for now. Look only at the four top edge stickers.\n"
                + "Use F R U R' U' F' to progress from dot to L, from L to line, and from line to cross.\n"
                + "For the line, hold it vertical. For the L, hold yellow edges at the back and left positions.\n\n"
                + "The step is complete when the yellow center and all four yellow edge stickers form a cross.",
                3,
                "BeginnerYellowCross",
                "Create a yellow cross while keeping the first two layers solved.",
                new[]
                {
                    AlgorithmCase(
                        "Case 1 / 3: Yellow Line",
                        "Hold the yellow line horizontally across the top face, then apply the yellow-cross algorithm once.",
                        "R U R' U R U2 R'",
                        new[] { "F", "R", "U", "R'", "U'", "F'" },
                        "Yellow top edges",
                        "Top yellow cross",
                        0, 1, 1,
                        0, 1, 1),
                    AlgorithmCase(
                        "Case 2 / 3: Yellow L",
                        "Hold the yellow L in the top-face corner shown by the demo, then apply the algorithm.",
                        "R U R' U R U2 R'",
                        new[] { "F", "U", "R", "U'", "R'", "F'" },
                        "Yellow L pattern",
                        "Top yellow cross",
                        -1, 1, 0,
                        -1, 1, 0),
                    AlgorithmCase(
                        "Case 3 / 3: Yellow Dot",
                        "With only the yellow center visible, repeat the algorithm through the intermediate edge patterns until a cross appears.",
                        "R U R' U R U2 R'",
                        new[]
                        {
                            "F", "R", "U", "R'", "U'", "F'",
                            "F", "R", "U", "R'", "U'", "F'",
                            "U",
                            "F", "R", "U", "R'", "U'", "F'"
                        },
                        "Four unoriented yellow edges",
                        "Top yellow cross",
                        0, 1, 1,
                        0, 1, 1)
                },
                "Only the four yellow edge stickers matter in this step.",
                "Do not try to solve the yellow corners yet.",
                "Repeat the same algorithm with the correct line or L orientation.",
                "The first two layers must remain solved.");
        }

        private static LearnLessonData YellowFaceLesson()
        {
            return BeginnerAlgorithmLesson(
                "beginner_yellow_face",
                "Step 5: Yellow Face",
                "Orient all four top corners so the entire top becomes yellow.",
                "Start with the yellow cross complete.\n\n"
                + "Place an unsolved yellow corner at the front-right of the top layer.\n"
                + "Apply R U R' U R U2 R'. Recheck the top face, rotate only U as needed, and repeat.\n"
                + "Corner positions may still be wrong after this step; only their yellow orientation matters.\n\n"
                + "The step is complete when all nine stickers on the top face are yellow.",
                4,
                "BeginnerYellowFace",
                "Turn the yellow cross into a complete yellow face.",
                new[]
                {
                    AlgorithmCase(
                        "Case 1 / 2: One Sune",
                        "Keep an unsolved yellow corner at top-front-right and perform the seven-move orientation algorithm.",
                        "F2 U R' L F2 L' R U F2",
                        new[] { "R", "U", "R'", "U", "R", "U2", "R'" },
                        "Unoriented yellow corner",
                        "Top yellow face",
                        1, 1, 1,
                        1, 1, 1),
                    AlgorithmCase(
                        "Case 2 / 2: Anti-Sune Direction",
                        "Use the mirrored corner orientation when the yellow stickers point in the opposite pattern.",
                        "F2 U R' L F2 L' R U F2",
                        new[] { "R", "U2", "R'", "U'", "R", "U'", "R'" },
                        "Opposite yellow corner pattern",
                        "Top yellow face",
                        1, 1, 1,
                        1, 1, 1)
                },
                "The yellow cross must stay intact.",
                "This step orients corners; it does not necessarily position them.",
                "Rotate only the top layer between repetitions.",
                "Finish with all nine top stickers yellow.");
        }

        private static LearnLessonData LastLayerCornersLesson()
        {
            return BeginnerAlgorithmLesson(
                "beginner_last_corners",
                "Step 6: Position Last Layer Corners",
                "Move the yellow corners into their correct locations.",
                "Keep the full yellow face on top.\n\n"
                + "A corner is correctly positioned when its three colors match the three surrounding centers, even if you imagine its orientation separately.\n"
                + "If one corner is correct, keep it at the front-right and apply the corner-positioning algorithm.\n"
                + "If none are correct, apply it once from any angle, then place the newly correct corner at front-right and repeat.\n\n"
                + "The step is complete when all four yellow corners belong in their current locations.",
                5,
                "BeginnerLastCorners",
                "Position every yellow corner while preserving the yellow face and first two layers.",
                new[]
                {
                    AlgorithmCase(
                        "Corner Positioning Demo",
                        "Cycle the top corners into their matching center-color locations. Repeat from the instructed reference angle when a real cube needs another cycle.",
                        "F2 U R' L F2 L' R U F2",
                        new[] { "L'", "U", "R", "U'", "L", "U", "R'" },
                        "Three misplaced yellow corners",
                        "Correct top-corner positions",
                        1, 1, -1,
                        -1, 1, 1)
                },
                "Judge a corner by all three colors, not only yellow.",
                "The yellow face should remain oriented.",
                "Use a correct corner as the reference at top-front-right.",
                "Side-face corner colors must match after completion.");
        }

        private static LearnLessonData LastLayerEdgesLesson()
        {
            return BeginnerAlgorithmLesson(
                "beginner_last_edges",
                "Step 7: Position Last Layer Edges",
                "Cycle the final yellow edges to solve the cube.",
                "All corners are now correct. Only the four top-layer edges may remain out of position.\n\n"
                + "If one side is already solved, hold that solved side at the back.\n"
                + "Choose the clockwise or counter-clockwise edge cycle according to where the remaining edges must move.\n"
                + "If no side is solved, perform one cycle, find the solved side, place it at the back, and finish.\n\n"
                + "The cube is complete when every face has one uniform color.",
                6,
                "BeginnerLastEdges",
                "Position the final four edges and finish the cube.",
                new[]
                {
                    AlgorithmCase(
                        "Case 1 / 2: Clockwise Edge Cycle",
                        "Keep the solved side at the back and cycle the remaining top edges clockwise.",
                        string.Empty,
                        new[] { "F2", "U", "R'", "L", "F2", "L'", "R", "U", "F2" },
                        "Three misplaced top edges",
                        "Solved cube",
                        0, 1, 1,
                        0, 1, 1),
                    AlgorithmCase(
                        "Case 2 / 2: Counter-clockwise Edge Cycle",
                        "Keep the solved side at the back and cycle the remaining top edges counter-clockwise.",
                        string.Empty,
                        new[] { "F2", "U'", "R'", "L", "F2", "L'", "R", "U'", "F2" },
                        "Three misplaced top edges",
                        "Solved cube",
                        0, 1, 1,
                        0, 1, 1)
                },
                "Do not rotate the whole cube after choosing the solved back side.",
                "Use the cycle direction that sends each edge toward its matching center.",
                "All corners must remain solved.",
                "Completion means every face is a single color.");
        }

        private static LearnLessonData BeginnerAlgorithmLesson(
            string lessonId,
            string title,
            string shortDescription,
            string bodyText,
            int order,
            string demoType,
            string purpose,
            LearnStepDemoData[] cases,
            params string[] keyPoints)
        {
            return new LearnLessonData
            {
                lessonId = lessonId,
                title = title,
                shortDescription = shortDescription,
                bodyText = bodyText,
                order = order,
                has3DDemo = true,
                demoType = demoType,
                demoPurpose = purpose,
                demoGoalDescription = shortDescription,
                moveNotations = Array.Empty<string>(),
                demoMoves = Array.Empty<string>(),
                demoSubsteps = cases,
                keyPoints = keyPoints
            };
        }

        private static LearnStepDemoData AlgorithmCase(
            string title,
            string instruction,
            string goalSetupNotation,
            string[] demoMoves,
            string pieceHint,
            string slotHint,
            int targetStartX,
            int targetStartY,
            int targetStartZ,
            int targetGoalX,
            int targetGoalY,
            int targetGoalZ)
        {
            var moveInstructions = new string[demoMoves.Length];
            for (int i = 0; i < moveInstructions.Length; i++)
            {
                moveInstructions[i] = $"{instruction}\nMove {i + 1}/{demoMoves.Length}: {demoMoves[i]}";
            }

            return new LearnStepDemoData
            {
                substepTitle = title,
                instructionText = instruction,
                goalSetupNotation = goalSetupNotation,
                startFaceletString = WhiteBottomSolved,
                demoMoves = demoMoves,
                moveInstructions = moveInstructions,
                targetDescription = instruction,
                highlightedCubieHint = pieceHint,
                targetSlotHint = slotHint,
                wholeCubeYawQuarterTurns = 2,
                generateSetupFromDemoMoves = true,
                targetStartX = targetStartX,
                targetStartY = targetStartY,
                targetStartZ = targetStartZ,
                targetGoalX = targetGoalX,
                targetGoalY = targetGoalY,
                targetGoalZ = targetGoalZ
            };
        }

        private static LearnStepDemoData CrossSubstep(
            string title,
            int wholeCubeYawQuarterTurns,
            string sideColorName,
            string[] demoMoves,
            string[] moveInstructions,
            int targetStartX,
            int targetStartY,
            int targetStartZ)
        {
            return new LearnStepDemoData
            {
                substepTitle = title,
                instructionText =
                    $"Find the white-{sideColorName} edge and keep the {sideColorName} center facing you. "
                    + "Prepare the edge on the top layer, align its side color with the center, "
                    + "then use F2 for the final insertion.",
                setupNotation = string.Empty,
                // Keeps the four bottom white edges solved while moving all four white corners away.
                goalSetupNotation = "B U' L' B' R U L R'",
                startFaceletString = WhiteBottomSolved,
                demoMoves = demoMoves,
                moveInstructions = moveInstructions,
                targetDescription =
                    $"Place the white-{sideColorName} edge into the bottom-{sideColorName} cross slot.",
                highlightedCubieHint = $"White-{sideColorName} edge",
                targetSlotHint = $"Bottom-{sideColorName} cross slot",
                targetSideColorName = sideColorName,
                wholeCubeYawQuarterTurns = wholeCubeYawQuarterTurns,
                generateSetupFromDemoMoves = true,
                targetStartX = targetStartX,
                targetStartY = targetStartY,
                targetStartZ = targetStartZ,
                targetGoalX = 0,
                targetGoalY = -1,
                targetGoalZ = 1
            };
        }

        private static void AddFaceNotation(
            ICollection<LearnLessonData> target,
            string notation,
            string faceName,
            int order)
        {
            target.Add(Notation(notation, faceName, "clockwise", order));
            target.Add(Notation(notation + "'", faceName, "counter-clockwise", order + 1));
            target.Add(Notation(notation + "2", faceName, "180 degrees", order + 2));
        }

        private static LearnLessonData Notation(string move, string faceName, string direction, int order)
        {
            return Demo(
                "notation_" + move.Replace("'", "prime").ToLowerInvariant(),
                move,
                $"Turn the {faceName.ToLowerInvariant()} face {direction}.",
                $"Look directly at the {faceName.ToLowerInvariant()} face. Clockwise and counter-clockwise are measured from that view.",
                order,
                "NotationMove",
                new[] { move },
                null,
                null,
                $"Watch the {move} move on a solved cube.",
                "Practice one notation move while keeping the whole cube orientation fixed.",
                null,
                null,
                "Keep the whole cube orientation fixed.");
        }

        private static LearnLessonData Formula(
            string id,
            string title,
            string description,
            int order,
            params string[] moves)
        {
            return Demo(
                id,
                title,
                description,
                "This demo shows the move pattern only. Use this formula when your cube has the correct case.",
                order,
                "FormulaPattern",
                moves,
                null,
                null,
                "Watch the complete algorithm, or step through one move at a time.",
                "Pattern demo only. This is not a full solving case.",
                null,
                null,
                "Accuracy comes before speed.",
                "Keep the same orientation for the whole sequence.");
        }

        private static LearnLessonData PendingStep(string id, string title, int order)
        {
            return new LearnLessonData
            {
                lessonId = id,
                title = title,
                shortDescription = "Lesson explanation is available; the dedicated 3D case is next.",
                bodyText = "This step remains part of the complete beginner method. Its dedicated start state and guided 3D demo will be added in the next Learn expansion.",
                order = order,
                has3DDemo = false,
                demoType = "ExplanationOnly",
                demoPurpose = "Explanation only. A dedicated solving case will be added later.",
                demoGoalDescription = "3D demo will be added in the next Learn expansion.",
                moveNotations = Array.Empty<string>(),
                demoMoves = Array.Empty<string>(),
                keyPoints = new[] { "Review the completed earlier steps before continuing." },
                isExpandedContent = true
            };
        }

        private static LearnLessonData Explanation(
            string id,
            string title,
            string description,
            string body,
            int order,
            params string[] points)
        {
            return new LearnLessonData
            {
                lessonId = id,
                title = title,
                shortDescription = description,
                bodyText = body,
                order = order,
                has3DDemo = false,
                demoType = "ExplanationOnly",
                demoPurpose = "Explain the concept without an unnecessary move animation.",
                demoGoalDescription = "3D demo is not required for this explanation.",
                moveNotations = Array.Empty<string>(),
                demoMoves = Array.Empty<string>(),
                keyPoints = points ?? Array.Empty<string>()
            };
        }

        private static LearnLessonData Demo(
            string id,
            string title,
            string description,
            string body,
            int order,
            string demoType,
            string[] moves,
            string setup,
            string startFacelets,
            string goal,
            string purpose,
            string highlightedCubieHint,
            string targetSlotHint,
            params string[] points)
        {
            return new LearnLessonData
            {
                lessonId = id,
                title = title,
                shortDescription = description,
                bodyText = body,
                order = order,
                has3DDemo = true,
                demoType = demoType,
                moveNotations = moves ?? Array.Empty<string>(),
                demoMoves = moves ?? Array.Empty<string>(),
                demoSetupNotation = setup,
                demoStartFaceletString = startFacelets,
                demoGoalDescription = goal,
                demoPurpose = purpose,
                highlightedFaceletIndexes = Array.Empty<int>(),
                highlightedCubieHint = highlightedCubieHint,
                targetSlotHint = targetSlotHint,
                keyPoints = points ?? Array.Empty<string>()
            };
        }

        private static LearnCategoryData Category(
            string id,
            string title,
            string description,
            int order,
            params LearnLessonData[] lessons)
        {
            List<LearnLessonData> items = lessons.ToList();
            foreach (LearnLessonData lesson in items)
            {
                lesson.categoryId = id;
            }

            return new LearnCategoryData
            {
                categoryId = id,
                title = title,
                description = description,
                order = order,
                lessons = items
            };
        }
    }
}
