using NUnit.Framework;
using UnityEngine;
namespace GrandNavigation.Gunnery.Tests
{
    public class OpticalObservationTests
    {
        [Test] public void DeliveryUsesCaptureTimeAndNeverReadsTrueVelocity()
        {
            var b=new FireControlObservationBuffer(); int reads=0;
            bool Tick(double t) => b.Update(t,4.5f,2,true,0,0,Vector3.zero,
                ()=>{reads++;return new Vector3((float)t*8,0,160);},
                ()=>throw new System.Exception("Optical fire control leaked true velocity"));
            Assert.IsFalse(Tick(0)); Assert.IsFalse(Tick(1.99));
            Assert.IsTrue(Tick(2)); Assert.That(b.Track.Timestamp,Is.Zero);
            Assert.That(b.Track.Velocity,Is.EqualTo(Vector3.zero));
            Tick(4.5); Tick(6.49); Assert.That(b.Track.ObservationCount,Is.EqualTo(1));
            Tick(6.5); Assert.That(b.Track.Timestamp,Is.EqualTo(4.5));
            Assert.That(b.Track.Velocity.x,Is.EqualTo(8).Within(.001));
            Tick(7);
            Assert.That(b.Track.EstimatePosition(7).x,Is.EqualTo(56).Within(.001));
            Assert.IsTrue(b.Track.Predict(7,Vector3.up*4,65,9.81f,250,out var point,out var flight));
            Assert.That(Vector3.Distance(point,new Vector3(8*(7+flight),0,160)),Is.LessThan(.02));
            Assert.That(reads,Is.EqualTo(2));
        }
        [Test] public void AccelerationAndTurnRemainUnknownUntilDeliveredAndUseSecantVelocity()
        {
            var b=new FireControlObservationBuffer();
            Vector3 position=new Vector3(0,0,160);
            void Tick(double t) => b.Update(t,4.5f,2,true,0,0,Vector3.zero,()=>position,()=>throw new System.Exception());
            Tick(0); Tick(2); position.x=36; Tick(4.5); Tick(6.5);
            Vector3 old=b.Track.Velocity; position+=new Vector3(-20,0,-20);
            Tick(8); Assert.That(b.Track.Velocity,Is.EqualTo(old));
            Tick(9); Tick(10.99); Assert.That(b.Track.Velocity,Is.EqualTo(old));
            Tick(11); Assert.That(b.Track.Velocity.x,Is.EqualTo(-20/4.5f).Within(.001));
            Assert.That(b.Track.Velocity.z,Is.EqualTo(-20/4.5f).Within(.001));
        }
        [Test] public void QuantizationIsDeterministicBoundedAndNotRandom()
        {
            Vector3 p=new Vector3(31,0,163);
            var a=FireControlObservationBuffer.QuantizePosition(Vector3.up*4,p,8,2);
            for(int i=0;i<20;i++) Assert.That(FireControlObservationBuffer.QuantizePosition(Vector3.up*4,p,8,2),Is.EqualTo(a));
            Assert.That(Vector3.Distance(a,p),Is.GreaterThan(.1).And.LessThan(5));
            Assert.That(a.y,Is.Zero);
        }
        [Test] public void CoarseConstantTargetRemainsLearnableWithoutAPerfectCenter()
        {
            var buffer=new FireControlObservationBuffer(); var origin=new Vector3(0,4,14);
            float sum=0,maxError=0; int count=0;
            for(int i=0;i<=180;i++)
            {
                float now=i*.1f;
                Vector3 actual=new Vector3(-70+8*now,0,115);
                buffer.Update(now,4.5f,2,true,8,2,origin,()=>actual,()=>throw new System.Exception());
                if(now<7) continue;
                Assert.IsTrue(buffer.Track.Predict(now,origin,65,9.81f,250,out var point,out var flight));
                float error=Vector3.Distance(point,actual+Vector3.right*8*flight);
                sum+=error; maxError=Mathf.Max(maxError,error);count++;
            }
            Debug.Log($"STRAIGHT OPTICAL: mean={sum/count:F2}m max={maxError:F2}m (no boundary turn)");
            Assert.That(sum/count,Is.GreaterThan(.5f).And.LessThan(8));
            Assert.That(maxError,Is.LessThan(15),"Finite optical precision should remain manually correctable.");
        }
        [Test] public void PauseAndInvalidClockCannotDeliverOrCaptureEarly()
        {
            var b=new FireControlObservationBuffer(); int reads=0;
            for(int i=0;i<20;i++) Assert.IsFalse(b.Update(0,4.5f,2,true,8,2,Vector3.zero,()=>{reads++;return Vector3.forward*160;},()=>Vector3.zero));
            Assert.That(reads,Is.EqualTo(1)); Assert.That(b.PendingCount,Is.EqualTo(1));
            Assert.IsFalse(b.Update(double.NaN,4.5f,2,true,8,2,Vector3.zero,()=>Vector3.zero,()=>Vector3.zero));
        }
    }
}

