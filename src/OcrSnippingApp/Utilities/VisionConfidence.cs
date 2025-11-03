
using Google.Cloud.Vision.V1;

namespace OcrSnippingApp
{
    public static class VisionConfidence
    {
        public static float? TryComputeAverageConfidence(AnnotateImageResponse res)
        {
            var doc = res?.FullTextAnnotation;
            if (doc == null) return null;

            int count = 0;
            double sum = 0;

            foreach (var page in doc.Pages)
                foreach (var block in page.Blocks)
                    foreach (var paragraph in block.Paragraphs)
                        foreach (var word in paragraph.Words)
                        {
                            if (word.Confidence > 0f)
                            {
                                sum += word.Confidence;
                                count++;
                            }
                        }

            return count > 0 ? (float)(sum / count) : null;
        }
    }
}
