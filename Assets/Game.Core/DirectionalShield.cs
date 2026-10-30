using System;
namespace Game.Core
{
    public enum ShieldSide { Front, Rear }
    /// <summary>Owned exclusively by DamageState. Legacy undirected hits use Front.</summary>
    public sealed class DirectionalShield
    {
        public float Maximum { get; private set; }=50f;
        public float Front { get; private set; }=25f;
        public float Rear { get; private set; }=25f;
        public float Fraction { get; private set; }=.5f;
        public float Desired { get; private set; }=.5f;
        public float RegenPerSecond { get; set; }
        public float FrontDelay { get; private set; }
        public float RearDelay { get; private set; }
        public float FrontFlash { get; private set; }
        public float RearFlash { get; private set; }
        public float Total => Front+Rear;
        public float FrontCapacity => Maximum*Fraction;
        public float RearCapacity => Maximum-FrontCapacity;
        public bool Transferring => Math.Abs(Fraction-Desired)>.00001f;
        // Exact lateral boundary is Front. Caller supplies source-position minus impact-position.
        public static ShieldSide Side(AimPoint forward,AimPoint toSource) => forward.X*toSource.X+forward.Y*toSource.Y>=0f?ShieldSide.Front:ShieldSide.Rear;
        public void SetDesired(float fraction) => Desired=(float)Math.Round(StatMath.Clamp(fraction,0f,1f)*4f)/4f;
        public void StepDesired(int step) => SetDesired(Desired+step*.25f);
        public void SetTotal(float total)
        {
            total=StatMath.Clamp(total,0f,Maximum);
            Front=total*Fraction;Rear=total-Front;
        }
        public void SetMaximum(float maximum,bool fillDelta=false)
        {
            if(maximum==Maximum)return;
            float total=Total+(fillDelta?maximum-Maximum:0f);
            Maximum=Math.Max(0f,maximum);SetTotal(total);
        }
        public void Initialize(float maximum)
        {
            Fraction=Desired=.5f;FrontDelay=RearDelay=FrontFlash=RearFlash=0f;
            Maximum=Math.Max(0f,maximum);SetTotal(Maximum);
        }
        public void Hit(ShieldSide side)
        {
            if(side==ShieldSide.Front){FrontDelay=3f;FrontFlash=.22f;}
            else {RearDelay=3f;RearFlash=.22f;}
        }
        public float Absorb(float damage,ShieldSide side,bool operational)
        {
            if(!operational)return damage;
            float hp=side==ShieldSide.Front?Front:Rear;
            float absorbed=Math.Min(hp,Math.Max(0f,damage));
            if(side==ShieldSide.Front)Front-=absorbed;else Rear-=absorbed;
            return damage-absorbed;
        }
        public void Restore(float amount)
        {
            float missingFront=FrontCapacity-Front,missingRear=RearCapacity-Rear;
            float missing=missingFront+missingRear;
            if(missing<=0f || amount<=0f)return;
            float restored=Math.Min(amount,missing);
            Front+=restored*missingFront/missing;Rear+=restored*missingRear/missing;
        }
        private void Transfer(float dt)
        {
            float delta=StatMath.Clamp(Desired-Fraction,-dt,dt);
            Fraction=StatMath.Clamp(Fraction+delta,0f,1f);
            if(Front>FrontCapacity){float excess=Front-FrontCapacity;Front=FrontCapacity;Rear+=excess;}
            if(Rear>RearCapacity){float excess=Rear-RearCapacity;Rear=RearCapacity;Front+=excess;}
        }
        public void Tick(float dt,bool operational)
        {
            if(dt<=0f || !operational)return;
            // Integrate at delay and transfer boundaries; capacity shares vary linearly during transfer.
            float left=dt;
            while(left>0f)
            {
                float step=left;
                if(FrontDelay>0f)step=Math.Min(step,FrontDelay);
                if(RearDelay>0f)step=Math.Min(step,RearDelay);
                if(Transferring)step=Math.Min(step,Math.Abs(Desired-Fraction));
                if(step<=0f)break;
                float oldFraction=Fraction;
                bool frontReady=FrontDelay<=0f,rearReady=RearDelay<=0f;
                Transfer(step);
                float share=(oldFraction+Fraction)*.5f;
                float rate=Math.Max(0f,RegenPerSecond);
                if(frontReady)Front=Math.Min(FrontCapacity,Front+rate*share*step);
                if(rearReady)Rear=Math.Min(RearCapacity,Rear+rate*(1f-share)*step);
                FrontDelay=Math.Max(0f,FrontDelay-step);RearDelay=Math.Max(0f,RearDelay-step);
                FrontFlash=Math.Max(0f,FrontFlash-step);RearFlash=Math.Max(0f,RearFlash-step);
                left=Math.Max(0f,left-step);
            }
        }
    }
}