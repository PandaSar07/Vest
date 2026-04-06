import { Section, SectionHeader } from '@/components/landing/Section'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

export function BlankDeveloperSection() {
  return (
    <Section id="custom-section">
      <SectionHeader
        eyebrow="Placeholder section"
        title="(Developer) Replace this with your real content"
        description="This section is intentionally left as a scaffold. Keep it long and visual to maintain the cinematic scroll pacing."
      />

      <div className="mx-auto mt-14 grid max-w-6xl grid-cols-1 gap-5 md:grid-cols-2">
        <Card className="gsap-reveal">
          <CardHeader>
            <CardTitle>What to put here</CardTitle>
          </CardHeader>
          <CardContent className="pt-0 text-sm text-slate-400">
            <ul className="list-disc space-y-2 pl-5">
              <li>Short product demo video / animated screenshots</li>
              <li>Customer logos and 2–3 quotes</li>
              <li>Before/after comparison (old workflow vs Vest workflow)</li>
              <li>One “signature” interaction (pin + text swap + parallax)</li>
            </ul>
          </CardContent>
        </Card>

        <Card className="gsap-reveal">
          <CardHeader>
            <CardTitle>Random words (delete later)</CardTitle>
          </CardHeader>
          <CardContent className="pt-0 text-sm text-slate-400">
            velvet horizon / quiet momentum / glass signal / deep focus / slow reveal / silver grid / calm precision / midnight metric
          </CardContent>
        </Card>
      </div>
    </Section>
  )
}

