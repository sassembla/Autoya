using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Miyamasu {
	public class MainThreadRunner : MonoBehaviour {
		private List<EnumPair> readyEnums = new List<EnumPair>();
		private List<EnumPair> runningEnums = new List<EnumPair>();
		private List<EnumPair> doneEnums = new List<EnumPair>();
		
		private object lockObj = new object();

		private struct EnumPair {
			public IEnumerator iEnum;
			public Action onDone;
			public EnumPair (IEnumerator iEnum, Action onDone) {
				this.iEnum = iEnum;
				this.onDone = onDone;
			}
		}
		void Update () {
			lock (lockObj) {
				foreach (var enumPair in readyEnums) {
					runningEnums.Add(enumPair);
				}
				readyEnums.Clear();
			}

			foreach (var runningEnum in runningEnums) {
				if (!runningEnum.iEnum.MoveNext()) {
					runningEnum.onDone();
					doneEnums.Add(runningEnum);
				}
			}

			if (0 < doneEnums.Count) {
				foreach (var doneEnum in doneEnums) {
					runningEnums.Remove(doneEnum);
				}
				doneEnums.Clear();
			}
		}

		public void Commit (IEnumerator iEnum, Action onDone) {
			lock (lockObj) {
				readyEnums.Add(new EnumPair(iEnum, onDone));
			}
		}
	}
}