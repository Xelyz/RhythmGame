using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Util
{
    public static void Transition(string toScene)
    {
        Transfer.fromScene = SceneManager.GetActiveScene().name;
        Transfer.toScene = toScene;
        SceneManager.LoadScene("LoadingScene", LoadSceneMode.Additive);
    }

    public static int Sign(float value)
    {
        return value > 0 ? 1 : value < 0 ? -1 : 0;
    }

    public static Chart GetChart(string chartData)
    {
        Chart chart = new()
        {
            notes = new()
        };
        List<TimingPoint> timingPoints = new();
        string[] lines = chartData.Split('\n');
        string reading = "";
        Vector2 lastPos = Vector2.zero;
        int n = 1;
        foreach (string line in lines)
        {
            if (line == "") continue;
            if (line.StartsWith("[TimingPoints]"))
            {
                reading = "TimingPoints";
                continue;
            }
            if (line.StartsWith("[HitObjects]"))
            {
                reading = "HitObjects";
                continue;
            }
            if (line.StartsWith("SliderMultiplier"))
            {
                Values.baseSlideSpeed = float.Parse(line.Split(" ")[1]);
                continue;
            }
            if (line.StartsWith("["))
            {
                reading = "";
                continue;
            }
            if (reading == "TimingPoints")
            {
                string[] data = line.Split(',');
                TimingPoint timingPoint = new()
                {
                    startTime = float.Parse(data[0]),
                    endTime = Mathf.Infinity,
                };

                if (timingPoints.Count > 0)
                {
                    TimingPoint last = timingPoints[^1];
                    last.endTime = timingPoint.startTime;
                    timingPoints[^1] = last;
                }

                if (int.Parse(data[6]) == 1)
                {
                    timingPoint.beatInterval = float.Parse(data[1]);
                    timingPoint.slideSpeed = 1f;
                }
                else
                {
                    timingPoint.beatInterval = timingPoints[^1].beatInterval;
                    timingPoint.slideSpeed = -100f / float.Parse(data[1]);
                }

                timingPoints.Add(timingPoint);
            }
            if (reading == "HitObjects")
            {
                string[] data = line.Split(',');

                int noteType = int.Parse(data[3]);

                Note note;
                if ((noteType & 1) == 1)
                {
                    Tap tap = new()
                    {
                        position = PivotMiddle(new(int.Parse(data[0]), int.Parse(data[1]))),
                        timeStamp = int.Parse(data[2]),
                        noteType = NoteType.Tap,
                        nthNote = n++
                    };
                    note = tap;
                }
                else if ((noteType >> 1 & 1) == 1)
                {
                    Slide slide = new()
                    {
                        position = PivotMiddle(new(int.Parse(data[0]), int.Parse(data[1]))),
                        timeStamp = int.Parse(data[2]),
                        noteType = NoteType.Slide,
                        nthNote = n++,
                    };

                    string[] pointStr = data[5].Split('|');
                    SlideShape shape = pointStr[0] switch
                    {
                        "L" => SlideShape.Linear,
                        "B" => SlideShape.Bezier,
                        "P" => SlideShape.Circle,
                        _ => SlideShape.Bezier,
                    };

                    pointStr[0] = $"{data[0]}:{data[1]}";

                    List<Vector3> points = new();
                    foreach (string coord in pointStr)
                    {
                        string[] xy = coord.Split(':');
                        Vector3 point = PivotMiddle(new(float.Parse(xy[0]), float.Parse(xy[1])));
                        if (points.Count != 0 && points[^1] == point)
                        {
                            slide.slideSegments.Add(new()
                            {
                                shape = shape,
                                points = points.ToArray()
                            });
                            points.Clear();
                        }
                        points.Add(point);
                    }
                    slide.slideSegments.Add(new()
                    {
                        shape = shape,
                        points = points.ToArray()
                    });
                    points.Clear();
                    
                    slide.fixedLength = float.Parse(data[7]);
                    slide.PreProcessSegments();

                    TimingPoint currTP = new();
                    // Assign beat interval based on the timing point the note is in
                    foreach (var timingPoint in timingPoints)
                    {
                        if (slide.timeStamp >= timingPoint.startTime && slide.timeStamp < timingPoint.endTime)
                        {
                            currTP = timingPoint;
                            break;
                        }
                    }
                    
                    slide.beatInterval = currTP.beatInterval;
                    slide.duration = slide.fixedLength / (Values.baseSlideSpeed * 100 * currTP.slideSpeed) * currTP.beatInterval;
                    note = slide;
                }
                else
                {
                    note = new Note();
                }

                chart.notes.Add(note);
            }
        }
        return chart;
    }

    public static Vector2 PivotMiddle(Vector2 pivotTopLeft)
    {
        return new Vector2(pivotTopLeft.x - 0.5f * Values.noteHolderWidth, -pivotTopLeft.y + 0.5f * Values.noteHolderHeight);
    }

    public static Color GetColor()
    {
        return new Color(255 / 255f, 255 / 255f, 255 / 255f);
    }

    public static Vector3 Bezier(float t, Vector3[] points)
    {
        int n = points.Length - 1;
        Vector3 result = Vector3.zero;

        for (int i = 0; i <= n; i++)
        {
            float binomialCoefficient = BinomialCoefficient(n, i);
            float blend = binomialCoefficient * Mathf.Pow(1 - t, n - i) * Mathf.Pow(t, i);
            result += blend * points[i];
        }

        return result;
    }

    private static int BinomialCoefficient(int n, int k)
    {
        if (k == 0 || k == n)
        {
            return 1;
        }

        int coefficient = 1;
        for (int i = 1; i <= k; i++)
        {
            coefficient *= n - (k - i);
            coefficient /= i;
        }

        return coefficient;
    }

    public static Vector3 Linear(float t, Vector3[] points)
    {
        if (points.Length != 2)
        {
            Debug.LogError("Linear interpolation requires exactly two points.");
        }

        return (1 - t) * points[0] + t * points[1];
    }

    public static Vector3 Circle(float t, Vector3[] points)
    {
        if (points.Length != 3)
        {
            Debug.LogError("Circle interpolation requires exactly three points.");
            return Vector3.zero;
        }

        // Extract 2D points from the 3D vectors (assuming z is uniform for all points)
        Vector2 p1 = new(points[0].x, points[0].y);
        Vector2 p2 = new(points[1].x, points[1].y);
        Vector2 p3 = new(points[2].x, points[2].y);
        float z = points[0].z; // Assuming uniform z for all points

        // Calculate the circumcenter of the triangle formed by p1, p2, and p3
        Vector2 mid1 = (p1 + p2) / 2;
        Vector2 mid2 = (p2 + p3) / 2;

        Vector2 dir1 = new(p2.y - p1.y, p1.x - p2.x); // Perpendicular direction to p1-p2
        Vector2 dir2 = new(p3.y - p2.y, p2.x - p3.x); // Perpendicular direction to p2-p3

        float t1 = ((mid2.x - mid1.x) * dir2.y - (mid2.y - mid1.y) * dir2.x) / (dir1.x * dir2.y - dir1.y * dir2.x);
        Vector2 circumCenter = mid1 + t1 * dir1;

        float radius = Vector2.Distance(p1, circumCenter);

        // Calculate start and end angles of the arc
        float startAngle = Mathf.Atan2(p1.y - circumCenter.y, p1.x - circumCenter.x);
        float passAngle = Mathf.Atan2(p2.y - circumCenter.y, p2.x - circumCenter.x);
        float endAngle = Mathf.Atan2(p3.y - circumCenter.y, p3.x - circumCenter.x);

        if (startAngle > passAngle)
        {
            if (startAngle < endAngle)
            {
                endAngle -= Mathf.PI * 2;
            }
            else if (passAngle < endAngle)
            {
                startAngle -= Mathf.PI * 2;
            }
        }
        else
        {
            if (startAngle > endAngle)
            {
                endAngle += Mathf.PI * 2;
            }
            else if (passAngle > endAngle)
            {
                startAngle += Mathf.PI * 2;
            }
        }

        // Interpolate angle based on t (from 0 to 1)
        float angle = Mathf.Lerp(startAngle, endAngle, t);

        // Calculate point on the circle in 2D
        Vector2 pointOnCircle2D = circumCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

        // Convert back to 3D, keeping the same z value
        Vector3 pointOnCircle = new(pointOnCircle2D.x, pointOnCircle2D.y, z);

        return pointOnCircle;
    }

    public delegate Vector3 CurveFunc(float t, Vector3[] points);

    public static CurveFunc GetCurveFunc(SlideShape shape)
    {
        return shape switch
        {
            SlideShape.Bezier => Bezier,
            SlideShape.Linear => Linear,
            SlideShape.Circle => Circle,
            _ => throw new("Unknown segment type"),
        };
    }
}