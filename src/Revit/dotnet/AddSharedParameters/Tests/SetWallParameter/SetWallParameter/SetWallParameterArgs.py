from assistant import controls

class Args(object):
    def __init__(self):
        self.set_parameter = controls.Bool('Set parameter', defaultValue=True)
        self.check_parameter = controls.Bool('Check parameter')

args = Args()